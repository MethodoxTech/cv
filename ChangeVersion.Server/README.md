# ChangeVersion.Server

Notice the server doesn't need to know anything about cv or `RepoStorage` at all, since its only job is to store "the latest". 

What's more, it's not really possible for the server to keep track of accurate date of "last update" time of files - it's more useful to keep an MD5 as checksum for avoiding uploading/downloading the same files.

## Server Architecture

Server uses ASP .NET Core minimal web API, without controllers. For authentication, we just use a basic "passcode" as `X-Api-Key` header.

There are a few benefits using ASP.Net Core instead of rolling a custom file server:

1. **Endpoints**
   * `GET  /files`
     Returns a JSON list of all tracked paths.
   * `GET  /files/{**path}`
     Streams the latest copy of a single file.
   * `PUT  /files/{**path}`
     Uploads or overwrites a file.
   * `DELETE /files/{**path}` (optional)
     Removes a file if you ever need cleanup.
2. **Storage**
   * Store each file under a root folder on disk (or in blob storage).
   * Mirror the path you get from the client (`/files/src/Program.cs` → `wwwroot/src/Program.cs`).
   * No history needed—just overwrite the old.
3. **Security**
   * HTTPS (Kestrel + certificate)
   * Simple token or API-key auth via middleware (e.g. an `X-Api-Key` header)
   * You can layer on ASP .NET Core Identity or JWT if you need per-user control down the road.
4. **Concurrency & Integrity**
   * Rely on Kestrel’s request-body streaming to handle large files.
   * If you want resume/partial uploads, either:
     * Break files into chunks on the client and `PUT /files/{path}?chunk=…`
     * Adopt an existing protocol like [tus.io](https://tus.io) (there’s a .NET server package)

## References

* For a discussion on server design: https://dev.to/methodox/devlog-20250806-change-version-file-changes-history-only-version-control-for-binary-assets-5hf4