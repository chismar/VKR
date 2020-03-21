"C:\Program Files\VideoLAN\VLC\vlc.exe" --random --loop "test_greenscreen.mp4" :sout=#gather:rtp{sdp=rtsp://:8554/} :network-caching=1500 :sout-all :sout-keep
