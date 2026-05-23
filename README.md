# SkyRoute

Flight Search & Booking module for the SkyRoute travel aggregator.
Senior Full-Stack Developer Challenge.

- **Backend:** ASP.NET Core (.NET 10), 3-layer architecture (Presentation /
  Business / Data Access), Strategy pattern for flight operators.
- **Frontend:** Angular 21 standalone, two **independent, exportable feature
  modules** (`search` and `book`) wired by a thin host shell.
- **Observability:** OpenTelemetry + Serilog → Aspire Dashboard.

---

## Quick start

### Docker (everything together)

```powershell
docker compose up
# Web:        http://localhost:4200
# API:        http://localhost:8080
# Dashboard:  http://localhost:18888
```

#### Local dev environment setup (Windows / DevOps)

Step-by-step for engineers setting up the stack locally on Windows:

1. **Install prerequisites.** Install [Docker Desktop](https://www.docker.com/products/docker-desktop/)
   and make sure the Docker engine is **running** (whale icon → "Docker Desktop is running").
2. **Open an elevated PowerShell.** Right-click PowerShell (or Windows Terminal)
   and choose **Run as administrator**. See the gotcha below.
3. **Clone and enter the repo.**

   ```powershell
   git clone https://github.com/rodrigoramirez93/sky-route.git
   cd sky-route
   ```

4. **Bring the stack up.**

   ```powershell
   docker compose up
   ```

5. **Open the access points** once the containers report healthy:

   - Application: <http://localhost:4200/>
   - Aspire Dashboard: <http://localhost:18888/>

> ⚠️ **Common gotcha — must run as administrator on Windows.** Docker Compose
> needs an **elevated terminal** on Windows. Without it the Docker client cannot
> connect to the daemon and `docker compose up` fails with:
>
> ```
> unable to get image 'mcr.microsoft.com/dotnet/aspire-dashboard:latest':
> error during connect: in the default daemon configuration on Windows,
> the docker client must be run with elevated privileges to connect:
> ... open //./pipe/docker_engine: Access is denied
> ```
>
> Close the terminal, reopen it via **Run as administrator**, and retry.
