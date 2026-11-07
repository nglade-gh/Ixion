/*
 * libgl_stub.c - Remplace libGL.so.1.
 * Charge OpenGL via EGL surfaceless au lieu du driver NVIDIA GLX.
 * Utiliser : LD_PRELOAD=./libgl_stub.so dotnet run
 * OU : remplacer libGL.so.1
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

/* État EGL global */
static EGLDisplay egl_display = EGL_NO_DISPLAY;
static EGLSurface egl_surface = EGL_NO_SURFACE;
static EGLContext egl_context = EGL_NO_CONTEXT;
static EGLConfig egl_config = NULL;
static int egl_ready = 0;

/* Fonction d'init EGL (appelée au premier appel OpenGL) */
static void ensure_egl(void)
{
    if (egl_ready) return;
    egl_ready = 1;

    /* 1. Obtenir l'affichage EGL via EGL_PLATFORM_SURFACELESS_MESA */
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
        fprintf(stderr, "[libgl_stub] eglGetDisplay failed\n");
        return;
    }

    int maj, min;
    if (!eglInitialize(egl_display, &maj, &min)) {
        fprintf(stderr, "[libgl_stub] eglInitialize failed\n");
        return;
    }
    fprintf(stderr, "[libgl_stub] EGL %d.%d OK\n", maj, min);

    /* 2. Config OpenGL */
    EGLint attrs[] = {
        EGL_SURFACE_TYPE, EGL_PBUFFER_BIT,
        EGL_RENDERABLE_TYPE, EGL_OPENGL_BIT,
        EGL_RED_SIZE, 8, EGL_GREEN_SIZE, 8,
        EGL_BLUE_SIZE, 8, EGL_ALPHA_SIZE, 8,
        EGL_DEPTH_SIZE, 24, EGL_STENCIL_SIZE, 8,
        EGL_NONE
    };
    EGLint n;
    if (!eglChooseConfig(egl_display, attrs, &egl_config, 1, &n)) {
        fprintf(stderr, "[libgl_stub] eglChooseConfig failed\n");
        return;
    }

    /* 3. Contexte OpenGL 4.6 */
    EGLint ctx[] = {
        EGL_CONTEXT_MAJOR_VERSION, 4,
        EGL_CONTEXT_MINOR_VERSION, 6,
        EGL_NONE
    };
    egl_context = eglCreateContext(egl_display, egl_config, EGL_NO_CONTEXT, ctx);
    if (egl_context == EGL_NO_CONTEXT) {
        fprintf(stderr, "[libgl_stub] eglCreateContext failed\n");
        return;
    }

    /* 4. Surface pbuffer 1024x768 */
    EGLint pbuf[] = { EGL_WIDTH, 1024, EGL_HEIGHT, 768, EGL_NONE };
    egl_surface = eglCreatePbufferSurface(egl_display, egl_config, pbuf);
    if (egl_surface == EGL_NO_SURFACE) {
        fprintf(stderr, "[libgl_stub] eglCreatePbufferSurface failed\n");
        return;
    }

    /* 5. Make current */
    if (!eglMakeCurrent(egl_display, egl_surface, egl_surface, egl_context)) {
        fprintf(stderr, "[libgl_stub] eglMakeCurrent failed\n");
        return;
    }

    fprintf(stderr, "[libgl_stub] OpenGL %s OK\n", glGetString(GL_VERSION));
}

/* === Fonctions OpenGL de base (redirigent vers EGL) === */

/* glGetString */
const char* glGetString(enum GLenum name)
{
    ensure_egl();
    return eglGetCurrentContext() ? _real_glGetString(name) : NULL;
}

/* glClear */
void glClear(enum GLenum mask) { ensure_egl(); _real_glClear(mask); }

/* glClearColor */
void glClearColor(GLclamp r, GLclamp g, GLclamp b, GLclamp a) {
    ensure_egl();
    _real_glClearColor(r, g, b, a);
}

/* glViewport */
void glViewport(GLint x, GLint y, GLsizei w, GLsizei h) {
    ensure_egl();
    _real_glViewport(x, y, w, h);
}

/* glReadPixels */
void glReadPixels(GLint x, GLint y, GLsizei w, GLsizei h,
                  enum GLenum format, enum GLenum type, void *pixels)
{
    ensure_egl();
    _real_glReadPixels(x, y, w, h, format, type, pixels);
}

/* glDrawArrays */
void glDrawArrays(enum GLenum mode, GLint first, GLsizei count) {
    ensure_egl();
    _real_glDrawArrays(mode, first, count);
}

/* glDrawElements */
void glDrawElements(enum GLenum mode, GLsizei count,
                    enum GLenum type, const void *indices) {
    ensure_egl();
    _real_glDrawElements(mode, count, type, indices);
}

/* glGenTextures */
void glGenTextures(GLsizei n, GLuint *textures) {
    ensure_egl();
    _real_glGenTextures(n, textures);
}

/* glBindTexture */
void glBindTexture(enum GLenum target, GLuint texture) {
    ensure_egl();
    _real_glBindTexture(target, texture);
}

/* glTexImage2D */
void glTexImage2D(enum GLenum target, GLint level, GLint internalFormat,
                  GLsizei width, GLsizei height, GLint border,
                  enum GLenum format, enum GLenum type, const void *pixels) {
    ensure_egl();
    _real_glTexImage2D(target, level, internalFormat, width, height,
                       border, format, type, pixels);
}

/* glGenFramebuffers */
void glGenFramebuffers(GLsizei n, GLuint *framebuffers) {
    ensure_egl();
    _real_glGenFramebuffers(n, framebuffers);
}

/* glBindFramebuffer */
void glBindFramebuffer(enum GLenum target, GLuint framebuffer) {
    ensure_egl();
    _real_glBindFramebuffer(target, framebuffer);
}

/* glFramebufferTexture2D */
void glFramebufferTexture2D(enum GLenum target, enum GLenum attachment,
                             enum GLenum textarget, GLuint texture, GLint level) {
    ensure_egl();
    _real_glFramebufferTexture2D(target, attachment, textarget, texture, level);
}

/* glCheckFramebufferStatus */
enum GLenum glCheckFramebufferStatus(enum GLenum target) {
    ensure_egl();
    return _real_glCheckFramebufferStatus(target);
}

/* glDeleteFramebuffers */
void glDeleteFramebuffers(GLsizei n, const GLuint *framebuffers) {
    ensure_egl();
    _real_glDeleteFramebuffers(n, framebuffers);
}

/* glDeleteTextures */
void glDeleteTextures(GLsizei n, const GLuint *textures) {
    ensure_egl();
    _real_glDeleteTextures(n, textures);
}

/* glFinish */
void glFinish(void) { ensure_egl(); _real_glFinish(); }

/* glFlush */
void glFlush(void) { ensure_egl(); _real_glFlush(); }

/* === Pointeurs vers les vraies fonctions === */
static void (*_real_glClear)(enum GLenum) = NULL;
static void (*_real_glClearColor)(GLclamp, GLclamp, GLclamp, GLclamp) = NULL;
static void (*_real_glViewport)(GLint, GLint, GLsizei, GLsizei) = NULL;
static void (*_real_glReadPixels)(GLint, GLint, GLsizei, GLsizei, enum GLenum, enum GLenum, void*) = NULL;
static void (*_real_glDrawArrays)(enum GLenum, GLint, GLsizei) = NULL;
static void (*_real_glDrawElements)(enum GLenum, GLsizei, enum GLenum, const void*) = NULL;
static void (*_real_glGenTextures)(GLsizei, GLuint*) = NULL;
static void (*_real_glBindTexture)(enum GLenum, GLuint) = NULL;
static void (*_real_glTexImage2D)(enum GLenum, GLint, GLint, GLsizei, GLsizei, GLint, enum GLenum, enum GLenum, const void*) = NULL;
static void (*_real_glGenFramebuffers)(GLsizei, GLuint*) = NULL;
static void (*_real_glBindFramebuffer)(enum GLenum, GLuint) = NULL;
static void (*_real_glFramebufferTexture2D)(enum GLenum, enum GLenum, enum GLenum, GLuint, GLint) = NULL;
static enum GLenum (*_real_glCheckFramebufferStatus)(enum GLenum) = NULL;
static void (*_real_glDeleteFramebuffers)(GLsizei, const GLuint*) = NULL;
static void (*_real_glDeleteTextures)(GLsizei, const GLuint*) = NULL;
static void (*_real_glFinish)(void) = NULL;
static void (*_real_glFlush)(void) = NULL;

/* glGetStringf */
const char* glGetStringf(GLenum name, GLint* len) {
    ensure_egl();
    return _real_glGetString(name);
}

/* glGetStringi */
const unsigned char* glGetStringi(enum GLenum name, GLuint index) {
    ensure_egl();
    return _real_glGetStringi(name, index);
}

/* Fonction de chargement dynamique des vraies fonctions OpenGL */
static void load_real_functions(void)
{
    static int loaded = 0;
    if (loaded) return;
    loaded = 1;

    /* Charger depuis libOpenGL.so.0 ou libGL.so.1 */
    static void *lib = NULL;
    if (!lib) {
        lib = dlopen("libOpenGL.so.0", RTLD_NOW);
        if (!lib) lib = dlopen("libGL.so.1", RTLD_NOW);
    }

    if (lib) {
        #define LOAD(sym) _real_##sym = (typeof(_real_##sym))dlsym(lib, #sym)
        LOAD(glClear);
        LOAD(glClearColor);
        LOAD(glViewport);
        LOAD(glReadPixels);
        LOAD(glDrawArrays);
        LOAD(glDrawElements);
        LOAD(glGenTextures);
        LOAD(glBindTexture);
        LOAD(glTexImage2D);
        LOAD(glGenFramebuffers);
        LOAD(glBindFramebuffer);
        LOAD(glFramebufferTexture2D);
        LOAD(glCheckFramebufferStatus);
        LOAD(glDeleteFramebuffers);
        LOAD(glDeleteTextures);
        LOAD(glFinish);
        LOAD(glFlush);
        #undef LOAD
    }

    /* Si on n'a pas pu charger depuis libOpenGL.so, utiliser le dlopen original */
    if (!_real_glClear) {
        void *orig = dlopen("libGL.so.1", RTLD_NOW | RTLD_GLOBAL);
        if (orig) {
            #define LOAD(sym) _real_##sym = (typeof(_real_##sym))dlsym(orig, #sym)
            LOAD(glClear);
            LOAD(glClearColor);
            LOAD(glViewport);
            LOAD(glReadPixels);
            LOAD(glDrawArrays);
            LOAD(glDrawElements);
            LOAD(glGenTextures);
            LOAD(glBindTexture);
            LOAD(glTexImage2D);
            LOAD(glGenFramebuffers);
            LOAD(glBindFramebuffer);
            LOAD(glFramebufferTexture2D);
            LOAD(glCheckFramebufferStatus);
            LOAD(glDeleteFramebuffers);
            LOAD(glDeleteTextures);
            LOAD(glFinish);
            LOAD(glFlush);
            #undef LOAD
        }
    }
}

/* glXCreateContext — MonoGame DesktopGL l'utilise */
GLXContext glXCreateContext(Display *dpy, XVisualInfo *vis,
                              GLXContext shareList, int direct)
{
    ensure_egl();
    return (GLXContext)egl_context;
}

/* glXMakeCurrent */
int glXMakeCurrent(Display *dpy, GLXDrawable drawable, GLXContext ctx)
{
    ensure_egl();
    (void)dpy; (void)drawable; (void)ctx;
    return eglMakeCurrent(egl_display, egl_surface, egl_surface, (EGLContext)ctx) ? 1 : 0;
}

/* glXSwapBuffers */
void glXSwapBuffers(Display *dpy, GLXDrawable drawable)
{
    ensure_egl();
    (void)dpy; (void)drawable;
    eglSwapBuffers(egl_display, egl_surface);
}

/* glXGetProcAddress */
void (*glXGetProcAddress(const GLubyte *procName))(void)
{
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

/* glXGetProcAddressARB */
void (*glXGetProcAddressARB(const GLubyte *procName))(void)
{
    return glXGetProcAddress(procName);
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
    return "GLX_EXT_swap_control GLX_ARB_multisample GLX_ARB_fbconfig_float";
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

/* glXCreateNewContext */
GLXContext glXCreateNewContext(Display *dpy, GLXFBConfig config,
                                 int render_type, GLXContext share_list, int direct)
{
    ensure_egl();
    (void)dpy; (void)config; (void)render_type; (void)share_list; (void)direct;
    return egl_context;
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

/* glXQueryExtensionString */
const char *glXQueryExtensionString(Display *dpy, int *err, int *ev)
{
    (void)dpy;
    if (err) *err = 0;
    if (ev) *ev = 0;
    return "GLX";
}

/* === Pointeurs d'export pour OpenGL de base === */

/* glGetString */
const char* _real_glGetString(GLenum name)
{
    ensure_egl();
    static void *lib = NULL;
    if (!lib) {
        lib = dlopen("libOpenGL.so.0", RTLD_NOW);
        if (!lib) lib = dlopen("libGL.so.1", RTLD_NOW);
    }
    if (lib) return (const char *(*)(GLenum))dlsym(lib, "glGetString")(name);
    return NULL;
}
