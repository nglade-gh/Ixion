/*
 * glx_stub.c - Stub GLX qui redirige vers EGL surfaceless.
 * Compile : gcc -shared -fPIC -o libglx_stub.so glx_stub.c -ldl
 * Utiliser : LD_PRELOAD=./libglx_stub.so dotnet run
 */

#define _GNU_SOURCE
#include <dlfcn.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdarg.h>

/* X11 headers */
#include <X11/Xlib.h>
#include <X11/Xutil.h>

/* GLX headers */
#include <GL/glx.h>

/* EGL headers */
#include <EGL/egl.h>
#include <EGL/eglext.h>

/* Fonction SDL2 */
typedef void* SDL_Window;
typedef void* SDL_GLContext;

/* Pointeurs vers les vraies fonctions GLX */
static void* (*real_dlopen)(const char*, int) = NULL;
static void* (*real_dlsym)(void*, const char*) = NULL;
static int (*real_dlclose)(void*) = NULL;
static void* (*real_dlopen_gl)(const char*, int) = NULL;

/* EGL state global */
static EGLDisplay egl_display = EGL_NO_DISPLAY;
static EGLSurface egl_surface = EGL_NO_SURFACE;
static EGLContext egl_context = EGL_NO_CONTEXT;
static EGLConfig egl_config = NULL;
static int egl_initialized = 0;

static void init_egl(void)
{
    if (egl_initialized) return;
    egl_initialized = 1;

    /* Initialiser l'affichage EGL */
    egl_display = eglGetDisplay(EGL_DEFAULT_DISPLAY);
    if (egl_display == EGL_NO_DISPLAY) {
        fprintf(stderr, "[glx_stub] eglGetDisplay failed\n");
        return;
    }

    int major, minor;
    if (!eglInitialize(egl_display, &major, &minor)) {
        fprintf(stderr, "[glx_stub] eglInitialize failed\n");
        return;
    }
    fprintf(stderr, "[glx_stub] EGL initialized v%d.%d\n", major, minor);

    /* Config RGBA 32-bit */
    EGLint configAttribs[] = {
        EGL_SURFACE_TYPE, EGL_WINDOW_BIT,
        EGL_RENDERABLE_TYPE, EGL_OPENGL_BIT,
        EGL_BLUE_SIZE, 8,
        EGL_GREEN_SIZE, 8,
        EGL_RED_SIZE, 8,
        EGL_ALPHA_SIZE, 8,
        EGL_DEPTH_SIZE, 24,
        EGL_STENCIL_SIZE, 8,
        EGL_NONE
    };

    EGLint numConfigs;
    if (!eglChooseConfig(egl_display, configAttribs, &egl_config, 1, &numConfigs)) {
        fprintf(stderr, "[glx_stub] eglChooseConfig failed, retrying without window bit\n");
        configAttribs[1] = EGL_PBUFFER_BIT;
        if (!eglChooseConfig(egl_display, configAttribs, &egl_config, 1, &numConfigs)) {
            fprintf(stderr, "[glx_stub] eglChooseConfig failed\n");
            return;
        }
    }

    /* Créer un contexte OpenGL */
    EGLint contextAttribs[] = {
        EGL_CONTEXT_MAJOR_VERSION, 4,
        EGL_CONTEXT_MINOR_VERSION, 6,
        EGL_NONE
    };

    egl_context = eglCreateContext(egl_display, egl_config, EGL_NO_CONTEXT, contextAttribs);
    if (egl_context == EGL_NO_CONTEXT) {
        fprintf(stderr, "[glx_stub] eglCreateContext failed\n");
        return;
    }

    /* Créer une surface surfaceless (2x2) */
    EGLint pbufferAttribs[] = {
        EGL_WIDTH, 2,
        EGL_HEIGHT, 2,
        EGL_NONE
    };

    egl_surface = eglCreatePbufferSurface(egl_display, egl_config, pbufferAttribs);
    if (egl_surface == EGL_NO_SURFACE) {
        fprintf(stderr, "[glx_stub] eglCreatePbufferSurface failed\n");
        return;
    }

    /* Make current */
    if (!eglMakeCurrent(egl_display, egl_surface, egl_surface, egl_context)) {
        fprintf(stderr, "[glx_stub] eglMakeCurrent failed\n");
        return;
    }

    fprintf(stderr, "[glx_stub] EGL context created successfully (OpenGL 4.6 via EGL)\n");
}

/* --- Stub GLX functions --- */

/* glXGetProcAddress - retourner les pointeurs EGL/GL si besoin */
void* glXGetProcAddress(const unsigned char* procName)
{
    init_egl();
    /* Essayer EGL d'abord, puis charger depuis libOpenGL.so */
    void* ptr = eglGetProcAddress((const char*)procName);
    if (ptr) return ptr;

    if (real_dlopen_gl == NULL) {
        real_dlopen_gl = dlopen("libOpenGL.so.0", RTLD_NOW | RTLD_GLOBAL);
        if (real_dlopen_gl == NULL) {
            real_dlopen_gl = dlopen("libGL.so.1", RTLD_NOW | RTLD_GLOBAL);
        }
    }
    if (real_dlopen_gl) {
        return dlsym(real_dlopen_gl, (const char*)procName);
    }
    return NULL;
}

const char* glXQueryExtensionString(Display* dpy)
{
    return "GLX";
}

const char* glXQueryExtensionsString(Display* dpy, int screen)
{
    static const char* exts = "GLX_EXT_swap_control GLX_ARB_multisample GLX_ARB_fbconfig_float GLX_EXT_framebuffer_sRGB";
    return exts;
}

int glXQueryVersion(Display* dpy, int* major, int* minor)
{
    *major = 1;
    *minor = 4;
    return 1;
}

int glXGetConfig(Display* dpy, XVisualInfo* vis, int attrib, int* value)
{
    /* Retourner des valeurs par défaut acceptables */
    switch (attrib) {
        case X_VIDEODEPTH:  *value = 24; break;
        case X_RENDER_TYPE: *value = GLX_RGBA_BIT; break;
        case X_DEPTH_SIZE:  *value = 24; break;
        case X_STENCIL_SIZE:*value = 8;  break;
        case X_ACCUM_RED_SIZE: case X_ACCUM_GREEN_SIZE:
        case X_ACCUM_BLUE_SIZE: case X_ACCUM_ALPHA_SIZE:
            *value = 8; break;
        default: *value = 0; break;
    }
    return 0;
}

XVisualInfo* glXChooseVisual(Display* dpy, int screen, int* attribList)
{
    /* Créer un XVisualInfo fictif - suffisant pour que MG passe l'initialisation */
    static XVisualInfo vi;
    memset(&vi, 0, sizeof(XVisualInfo));
    vi.screen = screen;
    vi.visualid = 1; /* Fake visual ID */
    vi.red_mask = 0xFF;
    vi.green_mask = 0xFF;
    vi.blue_mask = 0xFF;
    vi.depth = 24;
    return &vi;
}

GLXContext glXCreateContext(Display* dpy, XVisualInfo* vis,
                              GLXContext shareList, int direct)
{
    init_egl();
    return (GLXContext)egl_context;
}

int glXMakeCurrent(Display* dpy, GLXDrawable drawable, GLXContext ctx)
{
    init_egl();
    return eglMakeCurrent(egl_display, egl_surface, egl_surface, (EGLContext)ctx);
}

int glXMakeCurrentReadable(Display* dpy, GLXDrawable drawable, GLXContext ctx)
{
    return glXMakeCurrent(dpy, drawable, ctx);
}

int glXSwapBuffers(Display* dpy, GLXDrawable drawable)
{
    init_egl();
    return eglSwapBuffers(egl_display, egl_surface);
}

void glXCopyContext(Display* dpy, GLXContext src, GLXContext dst, unsigned long mask)
{
    /* Pas utilisé en pratique - noop */
}

void glXDestroyContext(Display* dpy, GLXContext ctx)
{
    /* Pas de cleanup EGL nécessaire - le contexte est partagé */
}

int glXIsDirect(Display* dpy, GLXContext ctx)
{
    return 1; /* On dit que c'est direct */
}

void glXSelectEvent(Display* dpy, Window window, unsigned long mask)
{
    /* Pas utilisé - noop */
}

void glXGetSelectedEvent(Display* dpy, Window window, unsigned long* mask)
{
    *mask = 0;
}

/* glXGetProcAddressARB - alias vers glXGetProcAddress */
void* glXGetProcAddressARB(const unsigned char* procName)
{
    return glXGetProcAddress(procName);
}

/* glXGetCurrentDisplay - retourner un fake display */
Display* glXGetCurrentDisplay(void)
{
    static Display fake_display;
    return &fake_display;
}
