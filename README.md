# BoardGames

Live version of this app: https://boardgames.alexyakunin.com/

"Board Games" is the new [Fusion] sample and a functional 
web app allowing you to play real-time multi-player board 
(or board-like) games.

The sample implements a number of features that are 
hard to implement without Fusion. In particular, you might notice
that real-time state sync works literally everywhere in it. 
Try opening the sample in two different browsers, sign in using 
different user accounts, and:
- Create and play a game in both windows
- Check out what happens with game lists when you're
  creating a game, making moves, or posting a chat message
- Try renaming your user & see its name changes everywhere - 
  even in chat mentions!
  
The sample currently implements:
- One game (Gomoku) - for now. I plan to add a couple more 
  a bit later - there is a common base API allowing to
  add new games with ease
- Game lobby, where you can track the state of games you
  participate in, browse open games created by other users 
  and join them
- Game chat, which supports mentions. In reality, there is
  an extendable message parser and modular renderer, that 
  currently supports user and game mentions.
- User online/offline status tracking. Notice that every 
  user badge displays it.
- User profile page, where you can edit your user name, add 
  MS/GitHub accounts, see all browser sessions, "kick" some
  of them or sign out from all of them.

Finally, the sample supports both both Blazor Server and 
Blazor WebAssembly modes.

The [live version] of this app is hosted on Google Cloud:
- Cloud Run runs its Docker image in 1-core/512MB RAM container
- Cloud PostgreSql stores the data; it also runs on
  the cheapest 1 core/3.75GB RAM host.

The most interesting part? **Everything you see here
was built by a single person in just 9 days.**
[The very first commit](https://github.com/servicetitan/Stl.Fusion.Samples/commit/546ae7597bc7fa3a0b3c7f3b84e3a463bc3fd28f)
cloning Fusion's Blazorise template was made on Feb 1, 
and I wrote the README describing what's already done
on Feb 10. 

Check out [Fusion] and its 
[other samples](https://github.com/servicetitan/Stl.Fusion.Samples)
if you want to learn more!

P.S. Pretty sure there are some bugs - if you'll find one, 
please open the issue!

[Fusion]: https://github.com/servicetitan/Stl.Fusion
[Live version]: https://boardgames.alexyakunin.com/
