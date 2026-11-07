/*
 * glx_replace.c - Remplace libGLX.so en interceptant les symboles GLX.
 * Compile : gcc -shared -fPIC -o /usr/lib/x86_64-linux-gnu/libGLX.so.0.0.0 glx_replace.c -ldl -lEGL
 * NOTE : On peut aussi utiliser LD_PRELOAD au lieu de remplacer le fichier.
 */

#define _GNU_SOURCE
#include <dlfcn.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include <EGL/egl.h>
#include <EGL/eglext.h>
#include <GL/gl.h>
#include <GL/glx.h>

#include <X11/Xlib.h>
#include <X11/Xutil.h>

/* État EGL global */
static EGLDisplay egl_display = EGL_NO_DISPLAY;
static EGLSurface egl_surface = EGL_NO_SURFACE;
static EGLContext egl_context = EGL_NO_CONTEXT;
static EGLConfig egl_config = NULL;
static int egl_ready = 0;

static void ensure_egl(void)
{
    if (egl_ready) return;
    egl_ready = 1;

    /* 1. Obtenir l'affichage EGL */
    typedef EGLDisplay (*PFNEGLGETPLATFORMDISPLAYEXTPROC)(EGLenum, void*, const EGLint*);
    PFNEGLGETPLATFORMDISPLAYEXTPROC getPlatformDisplay =
        (PFNEGLGETPLATFORMDISPLAYEXTPROC)eglGetProcAddress("eglGetPlatformDisplayEXT");

    if (getPlatformDisplay) {
        egl_display = getPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA, EGL_DEFAULT_DISPLAY, NULL);
    }
    if (egl_display == EGL_NO_DISPLAY) {
        egl_display = eglGetDisplay(EGL_DEFAULT_DISPLAY);
    }
    if (egl_display == EGL_NO_DISPLAY) {
        fprintf(stderr, "[glx_replace] eglGetDisplay failed\n");
        return;
    }

    int maj, min;
    eglInitialize(egl_display, &maj, &min);
    fprintf(stderr, "[glx_replace] EGL %d.%d OK\n", maj, min);

    /* 2. Config OpenGL 4.6 */
    EGLint attrs[] = {
        EGL_SURFACE_TYPE, EGL_PBUFFER_BIT,
        EGL_RENDERABLE_TYPE, EGL_OPENGL_BIT,
        EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8,
        EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
        EGL_DEPTH_SIZE, 24,
        EGL_NONE
    };
    EGLint n;
    eglChooseConfig(egl_display, attrs, &egl_config, 1, &n);

    EGLint ctx[] = {
        EGL_CONTEXT_MAJOR_VERSION, 4,
        EGL_CONTEXT_MINOR_VERSION, 6,
        EGL_NONE
    };
    egl_context = eglCreateContext(egl_display, egl_config, EGL_NO_CONTEXT, ctx);

    /* 3. Surface pbuffer */
    EGLint pbuf[] = { EGL_WIDTH, 1024, EGL_HEIGHT, 768, EGL_NONE };
    egl_surface = eglCreatePbufferSurface(egl_display, egl_config, pbuf);

    /* 4. Make current */
    eglMakeCurrent(egl_display, egl_surface, egl_surface, egl_context);
    fprintf(stderr, "[glx_replace] OpenGL %s — OK\n", glGetString(GL_VERSION));
}

/* === Symboles GLX requis par MonoGame === */

/* glXQueryVersion */
int glXQueryVersion(Display *dpy, int *major, int *minor) {
    if (major) *major = 1;
    if (minor) *minor = 4;
    return 1;
}

/* glXQueryExtensionsString */
const char *glXQueryExtensionsString(Display *dpy, int screen) {
    return "GLX_EXT_swap_control GLX_ARB_multisample GLX_ARB_fbconfig_float "
           "GLX_EXT_framebuffer_sRGB GLX_ARB_create_context GLX_EXT_create_context_es2_profile";
}

/* glXGetConfig */
int glXGetConfig(Display *dpy, XVisualInfo *vis, int attrib, int *value) {
    switch (attrib) {
        case GLX_RGBA:                *value = 1; break;
        case GLX_DOUBLEBUFFER:        *value = 1; break;
        case GLX_STENCIL_SIZE:        *value = 8; break;
        case GLX_DEPTH_SIZE:          *value = 24; break;
        case GLX_RED_SIZE:            *value = 8; break;
        case GLX_GREEN_SIZE:          *value = 8; break;
        case GLX_BLUE_SIZE:           *value = 8; break;
        case GLX_ALPHA_SIZE:          *value = 8; break;
        case GLX_ACCUM_RED_SIZE:      *value = 8; break;
        case GLX_ACCUM_GREEN_SIZE:    *value = 8; break;
        case GLX_ACCUM_BLUE_SIZE:     *value = 8; break;
        case GLX_ACCUM_ALPHA_SIZE:    *value = 8; break;
        case GLX_DRAWABLE_TYPE:       *value = GLX_WINDOW_BIT; break;
        case GLX_RENDER_TYPE:         *value = GLX_RGBA_BIT; break;
        default: *value = 0; break;
    }
    return 0;
}

/* glXChooseVisual */
XVisualInfo *glXChooseVisual(Display *dpy, int screen, int *attribList) {
    static XVisualInfo vi;
    memset(&vi, 0, sizeof(vi));
    vi.screen = 0;
    vi.visualid = 1;
    vi.class = TrueColor;
    vi.depth = 24;
    return &vi;
}

/* glXChooseFBConfig */
GLXFBConfig *glXChooseFBConfig(Display *dpy, int screen, const int *attribList, int *nelements) {
    static GLXFBConfig cfg = (GLXFBConfig)0x1;
    if (nelements) *nelements = 1;
    return &cfg;
}

/* glXGetFBConfigs */
GLXFBConfig *glXGetFBConfigs(Display *dpy, int screen, int *nelements) {
    static GLXFBConfig cfg = (GLXFBConfig)0x1;
    if (nelements) *nelements = 1;
    return &cfg;
}

/* glXGetVisualFromFBConfig */
XVisualInfo *glXGetVisualFromFBConfig(Display *dpy, GLXFBConfig config) {
    static XVisualInfo vi;
    (void)dpy; (void)config;
    memset(&vi, 0, sizeof(vi));
    vi.screen = 0;
    vi.visualid = 1;
    vi.class = TrueColor;
    vi.depth = 24;
    return &vi;
}

/* glXCreateContext */
GLXContext glXCreateContext(Display *dpy, XVisualInfo *vis, GLXContext share, int direct) {
    ensure_egl();
    return egl_context;
}

/* glXCreateNewContext */
GLXContext glXCreateNewContext(Display *dpy, GLXFBConfig config, int render_type, GLXContext share, int direct) {
    ensure_egl();
    return egl_context;
}

/* glXMakeCurrent */
int glXMakeCurrent(Display *dpy, GLXDrawable drawable, GLXContext ctx) {
    ensure_egl();
    (void)dpy; (void)drawable; (void)ctx;
    return eglMakeCurrent(egl_display, egl_surface, egl_surface, (EGLContext)ctx) ? 1 : 0;
}

/* glXMakeCurrentReadable */
int glXMakeCurrentReadable(Display *dpy, GLXDrawable drawable, GLXContext ctx) {
    return glXMakeCurrent(dpy, drawable, ctx);
}

/* glXGetCurrentContext */
GLXContext glXGetCurrentContext(void) {
    ensure_egl();
    return egl_context;
}

/* glXGetCurrentDisplay */
Display *glXGetCurrentDisplay(void) {
    static int fake_display_holder = 0;
    return (Display *)&fake_display_holder;
}

/* glXGetCurrentDrawable */
GLXDrawable glXGetCurrentDrawable(void) {
    static GLXDrawable fake = 1;
    return fake;
}

/* glXSwapBuffers */
void glXSwapBuffers(Display *dpy, GLXDrawable drawable) {
    ensure_egl();
    (void)dpy; (void)drawable;
    eglSwapBuffers(egl_display, egl_surface);
}

/* glXGetProcAddress / glXGetProcAddressARB */
void (*glXGetProcAddress(const GLubyte *procName))(void) {
    ensure_egl();
    (void)egl_display;
    void *p = eglGetProcAddress((const char *)procName);
    if (p) return (void (*)(void))p;

    static void *lib = NULL;
    if (!lib) {
        lib = dlopen("libOpenGL.so.0", RTLD_NOW | RTLD_GLOBAL);
        if (!lib) lib = dlopen("libGL.so.1", RTLD_NOW | RTLD_GLOBAL);
    }
    if (lib) return (void (*)(void))dlsym(lib, (const char *)procName);
    return NULL;
}

void (*glXGetProcAddressARB(const GLubyte *procName))(void) {
    return glXGetProcAddress(procName);
}

/* glXIsDirect */
int glXIsDirect(Display *dpy, GLXContext ctx) {
    return 1;
}

/* glXDestroyContext */
void glXDestroyContext(Display *dpy, GLXContext ctx) {
    (void)dpy; (void)ctx;
}

/* glXCopyContext */
void glXCopyContext(Display *dpy, GLXContext src, GLXContext dst, unsigned long mask) {
    (void)dpy; (void)src; (void)dst; (void)mask;
}

/* glXSelectEvent / glXGetSelectedEvent */
void glXSelectEvent(Display *dpy, Window window, unsigned long mask) {
    (void)dpy; (void)window; (void)mask;
}
void glXGetSelectedEvent(Display *dpy, Window window, unsigned long *mask) {
    (void)dpy; (void)window; if (mask) *mask = 0;
}

/* glXBindTexImage / glXReleaseTexImage */
void glXBindTexImage(Display *dpy, GLXDrawable drawable, int buffer) {
    (void)dpy; (void)drawable; (void)buffer;
}
void glXReleaseTexImage(Display *dpy, GLXDrawable drawable, int buffer) {
    (void)dpy; (void)drawable; (void)buffer;
}

/* glXQueryExtensionString */
const char *glXQueryExtensionString(Display *dpy, int *err, int *ev) {
    if (err) *err = 0;
    if (ev) *ev = 0;
    return "GLX";
}
