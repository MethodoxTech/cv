# ChangeVersion.Server

## Server Architecture

Server: ASP .NET Core Web API

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