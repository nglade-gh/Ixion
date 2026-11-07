/*
 * glx_intercept.c - Intercepte uniquement les fonctions GLX de MonoGame.
 * Les appels OpenGL (glClear, glDrawArrays, etc.) passent directement vers libOpenGL.so.0.
 * Compile : gcc -shared -fPIC -o glx_intercept.so glx_intercept.c -ldl -lEGL -lX11
 * Utiliser : LD_PRELOAD=./glx_intercept.so dotnet run
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

/* Charger libOpenGL.so.0 une fois pour les appels OpenGL directs */
static void *lib_opengl = NULL;

static void ensure_egl(void)
{
    if (egl_ready) return;
    egl_ready = 1;

    /* Charger libOpenGL.so.0 (Mesa EGL) */
    lib_opengl = dlopen("libOpenGL.so.0", RTLD_NOW | RTLD_GLOBAL);
    if (!lib_opengl) {
        fprintf(stderr, "[glx_intercept] Cannot load libOpenGL.so.0\n");
        return;
    }

    /* Obtenir eglGetPlatformDisplayEXT */
    typedef EGLDisplay (*PFN_GET_PLATFORM_DISPLAY)(EGLenum, void*, const EGLint*);
    PFN_GET_PLATFORM_DISPLAY getPlatformDisplay =
        (PFN_GET_PLATFORM_DISPLAY)eglGetProcAddress("eglGetPlatformDisplayEXT");

    if (getPlatformDisplay) {
        egl_display = getPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA, EGL_DEFAULT_DISPLAY, NULL);
    }
    if (egl_display == EGL_NO_DISPLAY) {
        egl_display = eglGetDisplay(EGL_DEFAULT_DISPLAY);
    }
    if (egl_display == EGL_NO_DISPLAY) {
        fprintf(stderr, "[glx_intercept] eglGetDisplay failed\n");
        return;
    }

    int maj, min;
    if (!eglInitialize(egl_display, &maj, &min)) {
        fprintf(stderr, "[glx_intercept] eglInitialize failed\n");
        return;
    }
    fprintf(stderr, "[glx_intercept] EGL %d.%d OK\n", maj, min);

    /* Config OpenGL */
    EGLint attrs[] = {
        EGL_SURFACE_TYPE, EGL_PBUFFER_BIT,
        EGL_RENDERABLE_TYPE, EGL_OPENGL_BIT,
        EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8,
        EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
        EGL_DEPTH_SIZE, 24,
        EGL_NONE
    };
    EGLint n;
    if (!eglChooseConfig(egl_display, attrs, &egl_config, 1, &n)) {
        fprintf(stderr, "[glx_intercept] eglChooseConfig failed\n");
        return;
    }

    /* Contexte OpenGL 4.6 */
    EGLint ctx[] = {
        EGL_CONTEXT_MAJOR_VERSION, 4,
        EGL_CONTEXT_MINOR_VERSION, 6,
        EGL_NONE
    };
    egl_context = eglCreateContext(egl_display, egl_config, EGL_NO_CONTEXT, ctx);
    if (egl_context == EGL_NO_CONTEXT) {
        fprintf(stderr, "[glx_intercept] eglCreateContext failed\n");
        return;
    }

    /* Pbuffer 1024x768 */
    EGLint pbuf[] = { EGL_WIDTH, 1024, EGL_HEIGHT, 768, EGL_NONE };
    egl_surface = eglCreatePbufferSurface(egl_display, egl_config, pbuf);
    if (egl_surface == EGL_NO_SURFACE) {
        fprintf(stderr, "[glx_intercept] eglCreatePbufferSurface failed\n");
        return;
    }

    /* Make current */
    if (!eglMakeCurrent(egl_display, egl_surface, egl_surface, egl_context)) {
        fprintf(stderr, "[glx_intercept] eglMakeCurrent failed\n");
        return;
    }

    fprintf(stderr, "[glx_intercept] OpenGL %s — OK\n", glGetString(GL_VERSION));
}

/* Charger dynamiquement un pointeur de fonction OpenGL depuis libOpenGL.so.0 */
static void *load_gl_func(const char *name)
{
    void *p = eglGetProcAddress(name);
    if (p) return p;
    if (lib_opengl) return dlsym(lib_opengl, name);
    return NULL;
}

/* === Interception de glXGetProcAddressARB ===
 * MonoGame 3.8 appelle ceci pour charger les extensions OpenGL.
 * On initialise EGL au premier appel et on retourne les pointeurs EGL. */
void (*glXGetProcAddressARB(const GLubyte *procName))(void)
{
    ensure_egl();
    if (!procName) return NULL;

    /* Essayer EGL d'abord (couvre toutes les extensions OpenGL) */
    void *p = eglGetProcAddress((const char *)procName);
    if (p) return (void (*)(void))p;

    /* Fallback vers libOpenGL.so.0 */
    return (void (*)(void))load_gl_func((const char *)procName);
}

/* Alias vers glXGetProcAddressARB */
void (*glXGetProcAddress(const GLubyte *procName))(void)
{
    return glXGetProcAddressARB(procName);
}

/* === Interception de glXCreateContext ===
 * Retourne le contexte EGL au lieu d'appeler le driver NVIDIA. */
GLXContext glXCreateContext(Display *dpy, XVisualInfo *vis,
                              GLXContext shareList, int direct)
{
    ensure_egl();
    return egl_context;
}

/* glXCreateNewContext */
GLXContext glXCreateNewContext(Display *dpy, GLXFBConfig config,
                                 int render_type, GLXContext share_list, int direct)
{
    ensure_egl();
    (void)dpy; (void)config; (void)render_type; (void)share_list; (void)direct;
    return egl_context;
}

/* glXMakeCurrent — fait le contexte EGL courant */
int glXMakeCurrent(Display *dpy, GLXDrawable drawable, GLXContext ctx)
{
    ensure_egl();
    (void)dpy; (void)drawable; (void)ctx;
    return eglMakeCurrent(egl_display, egl_surface, egl_surface, (EGLContext)ctx) ? 1 : 0;
}

/* glXMakeCurrentReadable */
int glXMakeCurrentReadable(Display *dpy, GLXDrawable drawable, GLXContext ctx)
{
    return glXMakeCurrent(dpy, drawable, ctx);
}

/* glXGetCurrentContext */
GLXContext glXGetCurrentContext(void)
{
    ensure_egl();
    return egl_context;
}

/* glXGetCurrentDisplay */
Display *glXGetCurrentDisplay(void)
{
    static int fake = 0;
    return (Display *)&fake;
}

/* glXGetCurrentDrawable */
GLXDrawable glXGetCurrentDrawable(void)
{
    static GLXDrawable fake = 1;
    return fake;
}

/* glXSwapBuffers */
void glXSwapBuffers(Display *dpy, GLXDrawable drawable)
{
    ensure_egl();
    (void)dpy; (void)drawable;
    eglSwapBuffers(egl_display, egl_surface);
}

/* glXQueryVersion */
int glXQueryVersion(Display *dpy, int *major, int *minor)
{
    (void)dpy;
    if (major) *major = 1;
    if (minor) *minor = 4;
    return 1;
}

/* glXQueryExtensionsString */
const char *glXQueryExtensionsString(Display *dpy, int screen)
{
    (void)dpy; (void)screen;
    return "GLX_EXT_swap_control GLX_ARB_multisample GLX_ARB_fbconfig_float "
           "GLX_EXT_framebuffer_sRGB GLX_ARB_create_context GLX_EXT_create_context_es2_profile";
}

/* glXChooseFBConfig */
GLXFBConfig *glXChooseFBConfig(Display *dpy, int screen, const int *attribList, int *nelements)
{
    (void)dpy; (void)screen; (void)attribList;
    static GLXFBConfig cfg = (GLXFBConfig)0x1;
    if (nelements) *nelements = 1;
    return &cfg;
}

/* glXGetFBConfigs */
GLXFBConfig *glXGetFBConfigs(Display *dpy, int screen, int *nelements)
{
    (void)dpy; (void)screen;
    static GLXFBConfig cfg = (GLXFBConfig)0x1;
    if (nelements) *nelements = 1;
    return &cfg;
}

/* glXChooseVisual */
XVisualInfo *glXChooseVisual(Display *dpy, int screen, int *attribList)
{
    (void)dpy; (void)attribList;
    static XVisualInfo vi;
    memset(&vi, 0, sizeof(vi));
    vi.screen = screen;
    vi.visualid = 1;
    vi.class = TrueColor;
    vi.depth = 24;
    return &vi;
}

/* glXGetVisualFromFBConfig */
XVisualInfo *glXGetVisualFromFBConfig(Display *dpy, GLXFBConfig config)
{
    (void)dpy; (void)config;
    static XVisualInfo vi;
    memset(&vi, 0, sizeof(vi));
    vi.screen = 0;
    vi.visualid = 1;
    vi.class = TrueColor;
    vi.depth = 24;
    return &vi;
}

/* glXGetConfig */
int glXGetConfig(Display *dpy, XVisualInfo *vis, int attrib, int *value)
{
    (void)dpy; (void)vis;
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

/* glXIsDirect */
int glXIsDirect(Display *dpy, GLXContext ctx)
{
    (void)dpy; (void)ctx;
    return 1;
}

/* glXDestroyContext */
void glXDestroyContext(Display *dpy, GLXContext ctx)
{
    (void)dpy; (void)ctx;
}

/* glXCopyContext */
void glXCopyContext(Display *dpy, GLXContext src, GLXContext dst, unsigned long mask)
{
    (void)dpy; (void)src; (void)dst; (void)mask;
}

/* glXSelectEvent / glXGetSelectedEvent */
void glXSelectEvent(Display *dpy, Window window, unsigned long mask)
{
    (void)dpy; (void)window; (void)mask;
}
void glXGetSelectedEvent(Display *dpy, Window window, unsigned long *mask)
{
    (void)dpy; (void)window; if (mask) *mask = 0;
}

/* glXBindTexImage / glXReleaseTexImage */
void glXBindTexImage(Display *dpy, GLXDrawable drawable, int buffer)
{
    (void)dpy; (void)drawable; (void)buffer;
}
void glXReleaseTexImage(Display *dpy, GLXDrawable drawable, int buffer)
{
    (void)dpy; (void)drawable; (void)buffer;
}

/* glXQueryExtensionString */
const char *glXQueryExtensionString(Display *dpy, int *err, int *ev)
{
    (void)dpy;
    if (err) *err = 0;
    if (ev) *ev = 0;
    return "GLX";
}
