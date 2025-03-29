# 🌐 CertOps - 인증서 자동화 시스템 / Automated Certificate Manager

**CertOps**는 Let's Encrypt 및 Azure DNS 기반의 SSL 인증서를 자동으로 발급하고 갱신하는 콘솔 애플리케이션입니다.  
CertOps is a console app that automates SSL/TLS certificate issuance and renewal using Let's Encrypt and Azure DNS.

---

## 📌 주요 기능 (Features)

- 🔐 **자동 인증서 발급 및 갱신 (Auto issue/renew certificates)**  
  ACME 프로토콜을 지원하는 Let's Encrypt와 연동하여 인증서를 자동으로 발급하고 갱신합니다.

- 🌐 **DNS-01 챌린지 지원 (DNS-01 challenge support)**  
  와일드카드 도메인(`*.domain.com`) 인증이 가능한 DNS-01 방식 사용

- ☁️ **Azure DNS 통합 (Azure DNS integration)**  
  Azure DNS를 통해 `_acme-challenge` TXT 레코드를 자동으로 생성/삭제합니다.

- 📦 **로그 및 인증서 아카이브 관리 (Log & Certificate Archiving)**  
  발급된 인증서, 개인 키, 로그를 지정된 볼륨 경로에 구조화하여 저장합니다.

---

## 🚀 시작하기 (Getting Started)

### 📦 사전 요구 사항 (Prerequisites)

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker](https://www.docker.com/)
- Azure DNS 설정 및 서비스 주체 (Azure DNS Zone + Client ID/Secret)

---

### ⚙️ 설정 파일 예시 (`config/config.json`)

```json
{
  "ScheduleDelayMilliseconds": 60000, // 인증서 만료 체크 주기 (단위: ms)
  "AcmeConfig": {
    "Domains": [
      "yourdomain.com",
      "*.yourdomain.com"
    ],
    "OutCertificatePath": "./live/", // 발급된 인증서 저장 경로
    "AccountConfig": {
      "Email": "you@example.com",         // Let's Encrypt 계정 이메일
      "AccountKeyPath": "./account/"      // 계정 키 저장 경로
    }
  },
  "AzureConfig": {
    "TenantId": "your-azure-tenant-id",             // Azure AD 테넌트 ID
    "ClientId": "your-azure-client-id",             // Azure App 등록 Client ID
    "ClientSecret": "your-azure-client-secret",     // Azure App 시크릿
    "AzureSubscriptionId": "your-subscription-id",  // Azure 구독 ID
    "AzureResourceGroup": "your-resource-group",    // DNS가 포함된 리소스 그룹
    "AzureDnsZone": "yourdomain.com"                // 도메인 이름 (DNS Zone)
  }
}
```



# 🌐 CertOps - Automated Certificate Manager

**CertOps** is a .NET console application that automates SSL/TLS certificate issuance and renewal using Let's Encrypt and Azure DNS.  
It is designed to replace manual Certbot flows with fully automated DNS-01 challenge handling and certificate archiving.

---

## 📌 Features

- 🔐 **Automatic Certificate Issuance and Renewal**  
  Automatically issues and renews certificates by interacting with Let's Encrypt via the ACME protocol.

- 🌐 **DNS-01 Challenge Support**  
  Supports wildcard certificates (`*.yourdomain.com`) by performing DNS-01 validation.

- ☁️ **Azure DNS Integration**  
  Automatically creates and removes `_acme-challenge` TXT records in Azure DNS to complete domain validation.

- 📦 **Logging and Certificate Archiving**  
  Stores issued certificates, private keys, and logs in well-structured volume paths for easy management and auditing.

---

## 🚀 Getting Started

### 📦 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Docker](https://www.docker.com/)
- Azure DNS Zone and Azure AD App Registration (Client ID/Secret)

---

### ⚙️ Configuration Example (`config/config.json`)

```json
{
  "ScheduleDelayMilliseconds": 60000,
  "AcmeConfig": {
    "Domains": [
      "yourdomain.com",
      "*.yourdomain.com"
    ],
    "OutCertificatePath": "./live/",
    "AccountConfig": {
      "Email": "you@example.com",
      "AccountKeyPath": "./account/"
    }
  },
  "AzureConfig": {
    "TenantId": "your-azure-tenant-id",
    "ClientId": "your-azure-client-id",
    "ClientSecret": "your-azure-client-secret",
    "AzureSubscriptionId": "your-subscription-id",
    "AzureResourceGroup": "your-resource-group",
    "AzureDnsZone": "yourdomain.com"
  }
}

