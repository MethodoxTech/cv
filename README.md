# Change Version (cv)

Change Version is a cli tool that provides quick check against a repo's changes without saving any changed contents. It does so by recording only the update time.

This is useful for cases when we DO NOT want to do version control yet would still want the capability to see what has changed, as in git.

## .cv

cv saves history inside the .cv folder. File update time is stored as utc.

## .gitignore

cv uses the same .gitignore file as git - but obviously the content is not saved.