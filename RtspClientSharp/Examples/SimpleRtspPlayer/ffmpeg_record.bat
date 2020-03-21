ffmpeg -f dshow -i video="screen-capture-recorder":audio="virtual-audio-capturer" -c:v libx264 -crf 0 -preset ultrafast output.mkv
