youtube-dl --update
youtube-dl -i --playlist-reverse --match-filter "duration > 1200" -o "%%(autonumber)s-%%(title)s.%%(ext)s" -x --audio-format m4a --embed-thumbnail https://www.youtube.com/playlist?list=UUaNx11M3bC-69l0P--J4flA