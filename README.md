# ShortTree

ShortTree is a compact URL shortener service built with ASP.NET Core, Entity Framework Core, and SQLite. It focuses on fast redirects and reliable click tracking, using background processing and caching to keep latency low while still capturing analytics.

## Overview

ShortTree lets you register custom, human-friendly slugs for long URLs and then redirect users to the original destination. Redirects are optimized by caching link lookups in memory, while click logs are queued in an unbounded channel and persisted by a background worker to avoid slowing the redirect path.

Key goals:
- Low-latency redirects with minimal database hits.
- Simple API-first design (JSON over HTTP).
- Durable analytics via click logs stored in SQLite.
- Clean separation of concerns using DI and hosted services.

## Architecture

### Request flow (redirect)
1. Client requests `GET /r/{username}/{slug}`.
2. The controller checks `IMemoryCache` for `{username}:{slug}`.
3. On cache miss, the link is loaded from SQLite and cached with sliding expiration.
4. The redirect is returned immediately.
5. A click log entry is queued to a `Channel` and written asynchronously by a background service.

This pattern ensures user-perceived latency is low while analytics remain reliable.

### Data model
- **User**: Owner of links. Unique username and email.
- **Link**: Custom slug for a real URL. Tracks total clicks.
- **ClickLog**: Individual click events (timestamp, IP, user agent, referrer).

### Background processing
- `ClickLogBackgroundService` reads from a channel and writes click logs to the database.
- The link's click counter is updated in the same transaction.

### Caching
- `IMemoryCache` stores `{username}:{slug} -> {linkId, longUrl}`.
- Sliding expiration keeps frequently used links hot and evicts inactive ones.

## API Endpoints

### Links
- `POST /api/links` - Create a link
- `GET /api/links/{username}/{slug}` - Get a single link
- `GET /api/links/{username}` - List all links for a user

### Redirect
- `GET /r/{username}/{slug}` - Redirect to the long URL and record a click

### Users
- `POST /api/user` - Create a user
- `GET /api/user/{username}` - Get a user
- `GET /api/user` - List users

### Stats
- `GET /api/stats/users` - Aggregate stats per user
- `GET /api/stats/users/{username}` - Stats for a specific user
- `GET /api/stats/clicks?username={u}&slug={s}&take={n}` - Click logs with filters

### Link Stats
- `GET /api/link-stats/{username}` - Click totals per link for a user
- `GET /api/link-stats/{username}/{slug}` - Click stats for a specific link

## Request examples

### Create a user
```json
POST /api/user
Content-Type: application/json

{
  "username": "ana",
  "email": "ana@example.com"
}
```

### Create a link
```json
POST /api/links
Content-Type: application/json

{
  "username": "ana",
  "title": "My blog",
  "longUrl": "https://example.com/blog",
  "slug": "blog",
  "visibleInProfile": true,
  "email": "ana@example.com"
}
```

### Redirect
```
GET /r/ana/blog
```

### User stats
```
GET /api/stats/users/ana
```

### Link stats
```
GET /api/link-stats/ana
```

## Configuration

### Connection string
SQLite uses a local file named `shorttree.db` by default:

```
"ConnectionStrings": {
  "DefaultConnection": "Data Source=shorttree.db"
}
```

### Cache settings
- Sliding expiration is currently set to 10 minutes in `RedirectController`.
- You can adjust this by changing the `CacheSlidingExpiration` value.

## Running locally

1. Restore and run:
   - `dotnet restore`
   - `dotnet run`
2. The database is created automatically on startup (`EnsureCreated`).
3. Use Postman or curl to test the endpoints.

## Notes and limitations

- Authentication/authorization is not implemented.
- `EnsureCreated` is used instead of migrations for simplicity.
- In-memory cache is per instance; multi-instance deployments should use a distributed cache or Redis.
- Click logging is best-effort; failures are logged but do not block redirects.

## Roadmap ideas

- Replace `EnsureCreated` with EF Core migrations.
- Add authentication and API keys.
- Add per-link analytics (daily/hourly aggregation).
- Introduce Redis for distributed cache and counters.
- Add rate limiting and abuse detection.

## License

MIT
