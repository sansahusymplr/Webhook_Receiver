# Webhook Receiver

ASP.NET Core webhook receiver with HMAC signature validation and idempotency.

## Endpoint

```
POST http://localhost:5102/webhooks/payment-succeeded
```

## Headers

- `X-Signature`: HMAC-SHA256 signature (hex lowercase)
- `X-Event-Id`: Unique event identifier

## Signature Generation

```csharp
var secret = "shared-secret";
var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), bodyBytes);
var hexSignature = Convert.ToHexString(signature).ToLower();
```

## Responses

- `200`: Event accepted or duplicate ignored
- `400`: Missing X-Event-Id header
- `401`: Missing or invalid signature

## Run

```bash
dotnet run
```
