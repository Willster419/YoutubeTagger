youtube-dl -i --playlist-reverse --dateafter 20190107 --match-filter "duration > 600" -o "%%(autonumber)s-%%(title)s.%%(ext)s" --format m4a --embed-thumbnail https://www.youtube.com/playlist?list=PLHJlC_5EPJgllwZxwJzYpG0w1Kz8DY3qY
