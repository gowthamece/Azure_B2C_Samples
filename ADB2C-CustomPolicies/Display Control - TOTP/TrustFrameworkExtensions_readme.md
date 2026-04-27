# B2C_1A_DisplayControlTrustFrameworkExtensions

This article gives an overview of the **B2C_1A_DisplayControlTrustFrameworkExtensions** custom policy. We recommend you to read the [Azure AD B2C custom policy overview](https://docs.microsoft.com/azure/active-directory-b2c/custom-policy-overview) and the [Display Controls documentation](https://learn.microsoft.com/en-us/azure/active-directory-b2c/display-controls) before reading this article.

## Policy Hierarchy

This extension policy inherits from `B2C_1A_DisplayControlTrustFrameworkLocalization` and adds the following customizations:

```
TrustFrameworkBase.xml
  └── TrustFrameworkLocalization.xml
        └── B2C_1A_DisplayControlTrustFrameworkExtensions  (this file)
              └── SignUpOrSignin.xml (Relying Party)
```

## Overview

The `B2C_1A_DisplayControlTrustFrameworkExtensions` policy extends the base policy with the following functionalities:

1. **Email Verification Using Display Controls** — Replaces the default email verification with a Display Control-based verification during sign-up.
2. **Duplicate Email Check During Sign-Up** — Prevents users from signing up with an email address that already exists in the directory.
3. **Custom Page Layout for Password Reset** — Overrides the `api.localaccountpasswordreset` content definition with a custom HTML page layout.
4. **Custom User Journey (SignUpOrSignIn-Custom)** — Defines a sign-up or sign-in user journey that uses the Display Control for email verification.
5. **Token Issuers** — Configures JWT, SAML, and session management technical profiles.


## Building Blocks

### Claims Schema

The following custom claim types are defined:

| Claim Type | Data Type | Purpose |
|---|---|---|
| `objectIdNotFound` | string | Used as a comparison value (`NOTFOUND`) to check whether a user exists in the directory. |
| `otp` | string | Stores the generated one-time password for email verification. |
| `verificationCode` | string | Stores the verification code entered by the user. Rendered as a TextBox on the page. |
| `strongAuthenticationEmailAddress` | string | Stores the email address used for sending the OTP via the Azure MFA protocol provider. |

### Claims Transformations

| Transformation Id | Method | Purpose |
|---|---|---|
| `AssertObjectIdObjectIdNotFoundAreEqual` | AssertStringClaimsAreEqual | Compares the `objectId` returned from AAD with the `objectIdNotFound` default value. If they are equal (both `NOTFOUND`), the user does not exist. If the user exists, the `objectId` will differ from `NOTFOUND`, causing the assertion to fail and throwing an error — preventing duplicate sign-ups. |
| `CopyEmailAddress` | FormatStringClaim | Copies the `email` claim value into the `strongAuthenticationEmailAddress` claim. This is required because the Azure MFA protocol provider expects the `strongAuthenticationEmailAddress` claim as input for sending OTP emails. |

### Content Definitions

| Content Definition Id | Purpose |
|---|---|
| `api.localaccountpasswordreset` | Overrides the forgot password page with a custom HTML page layout hosted on Azure Blob Storage. The custom page layout hides the "Change Email" button that appears after email verification in the Display Control. |

The content definition uses a custom `LoadUri` pointing to a custom HTML page:

```xml
<ContentDefinition Id="api.localaccountpasswordreset">
    <LoadUri>https://safuncappmcgentdev01.blob.core.windows.net/adb2clayout/B2C_ForgotPassword.html</LoadUri>
    <RecoveryUri>~/common/default_page_error.html</RecoveryUri>
    <DataUri>urn:com:microsoft:aad:b2c:elements:contract:selfasserted:2.1.26</DataUri>
</ContentDefinition>
```

**Note:** The `DataUri` must reference a valid selfasserted page contract version. Ensure the version specified is supported, otherwise you will receive the error: *"Sorry, but we're having trouble signing you in. AADB2C: An exception has occurred."*


### Display Controls

#### emailVerificationControl

The `emailVerificationControl` is a `VerificationControl` type Display Control that handles email verification during the sign-up flow. It replaces the default built-in email verification with a custom OTP-based flow.

```xml
<DisplayControl Id="emailVerificationControl" UserInterfaceControlType="VerificationControl">
```

**Display Claims:**
- `email` — The email address input field (required).
- `verificationCode` — The verification code input field mapped to `ControlClaimType="VerificationCode"` (required).

**Actions:**

The Display Control defines two actions:

1. **SendCode** — Executes the following validation technical profiles in sequence:
   - `AAD-UserReadUsingEmailAddress-RaiseIfExists` — Reads the directory to check if the email already exists. If the user exists, the assertion fails and an error is thrown, preventing the OTP from being sent.
   - `GenerateOtp` — Generates a 6-digit numeric OTP code with a 20-minute expiration (1200 seconds) and up to 5 retry attempts.
   - `SendOtp` — Sends the generated OTP to the user's email address using the Azure MFA protocol provider.

2. **VerifyCode** — Executes:
   - `VerifyOtp` — Validates the OTP code entered by the user against the generated code.


## Claims Providers

### Local Account SignIn

**login-NonInteractive**

Overrides the `login-NonInteractive` technical profile with tenant-specific `client_id` and `IdTokenAudience` values. These correspond to the `IdentityExperienceFramework` and `ProxyIdentityExperienceFramework` app registrations in the Azure AD B2C tenant.

```xml
<Item Key="client_id">acc288dc-da01-4136-87c7-31bdcf71b3e6</Item>
<Item Key="IdTokenAudience">37456243-5761-4514-b6af-429614c03359</Item>
```

### Token Issuers

| Technical Profile | Protocol | Purpose |
|---|---|---|
| `JwtIssuer` | OpenIdConnect | Issues JWT tokens. Uses `SM-jwt-issuer` for session management. |
| `SM-jwt-issuer` | Proprietary (OAuthSSOSessionProvider) | Manages OIDC-based token sessions. |
| `Saml2AssertionIssuer` | SAML2 | Issues SAML tokens. Uses `SM-Saml-issuer` for session management. |
| `SM-Saml-issuer` | Proprietary (SamlSSOSessionProvider) | Manages SAML-based token sessions. |

### One Time Password Technical Profiles

| Technical Profile | Operation | Purpose |
|---|---|---|
| `GenerateOtp` | GenerateCode | Generates a 6-digit numeric OTP. Code expires in 1200 seconds (20 minutes). Reuses the same code if requested again within the expiration window. Allows 5 retry attempts. |
| `VerifyOtp` | VerifyCode | Validates the verification code entered by the user against the generated OTP. |

### Email Sender

**SendOtp** — Sends the OTP email using the `AzureMfaProtocolProvider`. Before sending, it runs the `CopyEmailAddress` claims transformation to copy the `email` claim into `strongAuthenticationEmailAddress`, which is the claim expected by the Azure MFA provider.

### Self Asserted

**LocalAccountSignUpWithLogonEmail-CheckEmailAlreadyExists** — A Self-Asserted technical profile for the sign-up page. Key configurations:

- References the `emailVerificationControl` Display Control for email verification.
- Sets `EnforceEmailVerification` to `false` because the Display Control handles verification itself.
- Uses `api.localaccountsignup` as the content definition.
- Sets the continue button text to "Next" using `language.button_continue`.
- Displays a user-friendly error message if the email already exists: *"There is another user with this email address"*.

### Azure Active Directory

**AAD-UserReadUsingEmailAddress-RaiseIfExists** — Reads the directory using the email address as a sign-in name. This technical profile is used by the `emailVerificationControl` Display Control to check whether a user already exists before sending the OTP.

Key behavior:
- `RaiseErrorIfClaimsPrincipalDoesNotExist` is set to `false` — it does not throw an error if the user is not found.
- If the user is **not found**, `objectId` defaults to `NOTFOUND`.
- `objectIdNotFound` always defaults to `NOTFOUND`.
- The `AssertObjectIdObjectIdNotFoundAreEqual` claims transformation compares these two values. If both are `NOTFOUND` (user does not exist), the assertion passes and the OTP is sent. If the user **exists**, `objectId` contains the actual object ID, the assertion fails, and an error is thrown — preventing duplicate sign-ups.


## User Journey: SignUpOrSignIn-Custom

The custom user journey `SignUpOrSignIn-Custom` defines 4 orchestration steps:

| Step | Type | Description |
|---|---|---|
| **1** | CombinedSignInAndSignUp | Displays the sign-in page using `SelfAsserted-LocalAccountSignin-Email`. Provides the option for users to sign up via the sign-up link. Uses `api.signuporsignin` content definition. |
| **2** | ClaimsExchange | If the user clicks the sign-up link (no `objectId` claim exists), redirects to `LocalAccountSignUpWithLogonEmail-CheckEmailAlreadyExists` which presents the email verification Display Control. |
| **3** | ClaimsExchange | Reads additional user attributes from the directory using `AAD-UserReadUsingObjectId`. |
| **4** | SendClaims | Issues a JWT token to the relying party using `JwtIssuer`. |

**Preconditions:**
- Step 2 is skipped if the user successfully signs in (i.e., `objectId` already exists from Step 1).

**Note:** This user journey handles only sign-up and sign-in. The password reset flow is handled separately via the `PasswordResetSubJourney` defined in other extension policies.


## Summary

The `B2C_1A_DisplayControlTrustFrameworkExtensions` policy extends the base policy with a Display Control-based email verification flow for sign-up. It uses OTP generation and verification via the Azure MFA protocol provider and prevents duplicate sign-ups by checking the directory before sending the verification code. The policy also configures JWT and SAML token issuers, overrides the forgot password page layout with a custom HTML template, and defines a custom `SignUpOrSignIn-Custom` user journey with 4 orchestration steps.
