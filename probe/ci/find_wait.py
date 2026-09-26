#!/usr/bin/env python3
"""Read a uiautomator XML dump on stdin; print 'x y' centre of the ANR dialog's
'Wait' button (by resource-id android:id/aerr_wait, or by text 'Wait')."""
import re, sys
s = sys.stdin.read()
for node in re.findall(r"<node [^>]*>", s):
    if 'resource-id="android:id/aerr_wait"' in node or 'text="Wait"' in node:
        m = re.search(r'bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"', node)
        if m:
            x1, y1, x2, y2 = map(int, m.groups())
            print((x1 + x2) // 2, (y1 + y2) // 2)
            break
