#!/usr/bin/env python3
"""Dev server for the Unity WebGL build.

The build ships pre-compressed (.br) with no decompression fallback, so the
server must send Content-Encoding: br and the real Content-Type of the inner
file — a plain `python -m http.server` serves black screens.

Usage: python3 serve_play.py [port]   (default 8090, serves Builds/WebGL)
"""
import os
import sys
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "Builds", "WebGL")
PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8090

INNER_TYPES = {
    ".wasm": "application/wasm",
    ".js": "application/javascript",
    ".data": "application/octet-stream",
    ".json": "application/json",
}


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=ROOT, **kwargs)

    def end_headers(self):
        # cloudflared/browsers must not re-encode or range-split these
        self.send_header("Cache-Control", "no-cache")
        super().end_headers()

    def guess_type(self, path):
        if path.endswith(".br"):
            inner = os.path.splitext(path[:-3])[1]
            return INNER_TYPES.get(inner, "application/octet-stream")
        if path.endswith(".wasm"):
            return "application/wasm"
        return super().guess_type(path)

    def send_head(self):
        f = super().send_head()
        return f

    def send_response_only(self, code, message=None):
        super().send_response_only(code, message)

    def send_header(self, keyword, value):
        # Inject Content-Encoding alongside the Content-Type we computed.
        super().send_header(keyword, value)
        if keyword.lower() == "content-type" and self.path.split("?")[0].endswith(".br"):
            super().send_header("Content-Encoding", "br")


if __name__ == "__main__":
    if not os.path.isdir(ROOT):
        sys.exit(f"No build at {ROOT} — run ./build_and_deploy_webgl.sh --build-only first")
    print(f"Serving {ROOT}")
    print(f"→ http://localhost:{PORT}")
    ThreadingHTTPServer(("0.0.0.0", PORT), Handler).serve_forever()
