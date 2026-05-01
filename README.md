# DocuMark

**Convert Office documents and PDFs to clean Markdown with a modern web interface.**

DocuMark is a full-stack application that accepts `.docx`, `.xlsx`, `.pptx`, and `.pdf` files and extracts their content as formatted Markdown or AI-friendly summaries. It combines a **Next.js 16** frontend with a **.NET 8** backend, deployed on **AWS EC2** via **Terraform**.

---

## Features

- 📄 **Office to Markdown conversion** — Upload `.docx`, `.xlsx`, `.pptx`, and `.pdf` files with AI-friendly output
- 🧾 **Conversion logs** — Every export starts with a structured Markdown log header
- 🎨 **Modern UI** — Responsive frontend built with Next.js, Tailwind CSS, and TypeScript
- ⚡ **Fast backend** — .NET 8 Minimal API using OpenXML for efficient document parsing
- 🐳 **Docker support** — Full containerization for consistent local and production environments
- ☁️ **Cloud-ready** — Terraform provisioning for AWS EC2 with automatic deployment

---

## Project Structure

```
DocuMark/
├── backend/                          # .NET 8 API
│   ├── Program.cs                   # Main API entry point & routes
│   ├── DocuMark.Api.csproj          # Project file & dependencies
│   ├── DocuMark.Api.http            # Postman-style request examples
│   ├── Properties/launchSettings.json # Local dev settings
│   └── Dockerfile                   # Multi-stage .NET build
│
├── frontend/                         # Next.js 16 Application
│   ├── app/
│   │   ├── page.tsx                 # Main upload UI (Client Component)
│   │   ├── layout.tsx               # Root layout & metadata
│   │   ├── globals.css              # Tailwind CSS imports
│   │   └── api/convert/route.ts     # Proxy route to backend
│   ├── package.json                 # Dependencies & scripts
│   ├── tsconfig.json                # TypeScript config
│   ├── next.config.ts               # Next.js config
│   ├── Dockerfile                   # Multi-stage Node build
│   └── eslint.config.mjs            # Linting rules
│
├── docker-compose.yml               # Local dev & CI/CD orchestration
├── main.tf                          # Terraform AWS infrastructure
├── .gitignore                       # Git ignore rules
└── README.md                        # This file
```

---

## Technology Stack

### Frontend
- **Next.js 16.2.4** — React 19 with App Router
- **TypeScript 5** — Type-safe development
- **Tailwind CSS 4** — Utility-first styling
- **Node 20+** — Runtime

### Backend
- **.NET 8** — Modern C# framework
- **OpenXML SDK 3.0.2** — Document parsing
- **Swagger UI** — API documentation
- **CORS** — Cross-origin resource sharing

### Infrastructure
- **Docker & Docker Compose** — Containerization
- **Terraform 1.15+** — Infrastructure as Code (IaC)
- **AWS EC2** — Free tier `t3.micro` instance
- **AWS Security Groups** — Firewall rules

---

## Getting Started

### Prerequisites

- **Node.js 20+** & npm
- **.NET SDK 8.0**
- **Docker & Docker Compose** (for containerized runs)
- **Terraform 1.15+** (for AWS deployment)

### Local Development

#### 1. Clone the repository

```bash
git clone https://github.com/R20T04G/DocuMark.git
cd DocuMark
```

#### 2. Set up the backend

```bash
cd backend
dotnet restore
dotnet build
```

#### 3. Set up the frontend

```bash
cd ../frontend
npm install
npm run build
```

#### 4. Run both services

In separate terminals:

**Terminal 1 — Backend:**
```bash
cd backend
dotnet watch run --project DocuMark.Api.csproj
```

**Terminal 2 — Frontend:**
```bash
cd frontend
npm run dev
```

The frontend will be available at **http://localhost:3000**

---

## Docker Deployment

### Build and Run with Docker Compose

```bash
docker compose build
docker compose up -d
```

Then open **http://localhost:3000** in your browser.

The compose file:
- Builds both backend and frontend images
- Sets up internal networking (`http://backend:8080`)
- Exposes frontend on port `3000`
- Exposes backend on port `5152`

#### Environment Variables

Set in `docker-compose.yml`:

- **`ASPNETCORE_ENVIRONMENT=Development`** — Backend mode
- **`ASPNETCORE_URLS=http://+:8080`** — Backend listening address
- **`BACKEND_URL=http://backend:8080`** — Frontend proxy target

#### Cleanup

```bash
docker compose down -v
```

---

## AWS Deployment with Terraform

### Prerequisites

- AWS account with CLI configured (`aws configure`)
- Terraform installed

### Deploy to AWS

```bash
terraform init
terraform plan
terraform apply
```

When prompted, type `yes` to confirm.

**What Terraform creates:**
- 1 Security Group (firewall) allowing HTTP (80) and Next.js (3000) traffic
- 1 EC2 `t3.micro` instance (free tier eligible)
- Automatic user data script that:
  - Installs Docker and Docker Compose
  - Clones this repository
  - Runs `docker-compose up -d`

### Get the Public IP

```bash
terraform output server_public_ip
```

Visit `http://<public-ip>:3000` to access your app.

### Destroy Infrastructure

```bash
terraform destroy
```

---

## API Documentation

### Upload Endpoint

**POST** `/api/convert`

#### Request

```bash
curl -X POST -F "file=@document.docx" http://localhost:5152/api/convert
```

#### Response (Success)

```json
{
  "message": "Conversion successful!",
  "markdown": "# Document Title\n\nParagraph content here...\n"
}
```

#### Response (Error)

```json
{
  "message": "Supported formats are .docx, .xlsx, .pptx, and .pdf."
}
```

**Supported formats:** `.docx`, `.xlsx`, `.pptx`, `.pdf`

---

## Workflow

1. **User uploads a .docx, .xlsx, .pptx, or .pdf file** via the web interface
2. **Frontend posts to `/api/convert`** (Next.js route)
3. **Next.js proxy forwards** to backend at `http://backend:8080/api/convert`
4. **.NET backend parses** the document using OpenXML SDK or PdfPig
5. **Markdown log and extracted content** are returned as JSON
6. **Frontend displays** the Markdown in a formatted preview and download card

---

## File Format Support

| Format | Status | Notes |
|--------|--------|-------|
| `.docx` | ✅ Supported | Full support for text extraction |
| `.xlsx` | ✅ Supported | Workbook sheets converted to Markdown tables |
| `.pptx` | ✅ Supported | Slides converted to AI-friendly outlines |
| `.pdf` | ✅ Supported | Page text extraction |

---

## Development

### Frontend Scripts

```bash
npm run dev        # Start dev server (http://localhost:3000)
npm run build      # Build for production
npm run start      # Run production build
npm run lint       # Run ESLint
```

### Backend Scripts

```bash
dotnet build       # Compile
dotnet watch run   # Auto-recompile on file changes
dotnet test        # Run tests (if configured)
```

### Docker Commands

```bash
docker compose build                # Build images
docker compose up -d                # Start in background
docker compose logs -f frontend     # Watch frontend logs
docker compose logs -f backend      # Watch backend logs
docker compose down                 # Stop and remove containers
```

---

## Environment Variables

### Frontend (`.env.local`)

```env
BACKEND_URL=http://localhost:5152
```

Used by the proxy route in development. In Docker, set via `docker-compose.yml`.

### Backend (`appsettings.Development.json`)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Troubleshooting

### Frontend can't reach backend

1. Check backend is running: `curl http://localhost:5152/swagger`
2. Verify `BACKEND_URL` env var in proxy route
3. In Docker, ensure containers are on the same network: `docker network ls`

### .docx upload fails

1. Ensure file is a valid Word document (not corrupted)
2. Check file size — currently no limit enforced, but use reasonable sizes
3. View backend logs: `docker compose logs backend`

### Terraform apply fails

1. Ensure AWS credentials are configured: `aws sts get-caller-identity`
2. Check region is `us-east-1`: `aws configure get region`
3. Verify free tier eligibility for your account

---

## Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Commit changes: `git commit -m "Add feature"`
3. Push to remote: `git push origin feature/my-feature`
4. Open a Pull Request

---

## License

Unlicensed — Use freely.

---

## Support

For issues, questions, or feedback:
- Open an issue on GitHub
- Check existing documentation in `/docs`
- Review API examples in `backend/DocuMark.Api.http`

---

**Last Updated:** May 1, 2026
