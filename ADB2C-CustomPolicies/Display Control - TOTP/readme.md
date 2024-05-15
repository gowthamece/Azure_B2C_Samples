**TrustFramweworkExtensions_TOTP.xml**

  The extension file was created by referring https://github.com/azure-ad-b2c/samples/blob/master/policies/totp/policy/TrustFrameworkExtensions_TOTP.xml 

  **Modifications:** 

  1. Created a Signup user journey 
        
  2. Used {OAUTH-KV:email} as a default value for emailVerificationControl display control input claim to pre-populate the email address field based on the query string value 
       **Example:** 
       https://gowthamcbe.b2clogin.com/gowthamcbe.onmicrosoft.com/oauth2/v2.0/authorize?p=B2C_1A_DEMO_SIGNUP_SIGNIN_TOTP&client_id=08294166-1ef2-420c-8826-09d2810041b0&nonce=defaultNonce&redirect_uri=https%3A%2F%2Fjwt.ms&scope=openid&response_type=id_token&prompt=login&email=gowthamkk7@gmail.com 

      **Note:** I have written an article https://gowthamcbe.com/2024/05/03/azure-ad-b2c-custom-policy-to-pre-populate-email-field-in-sign-up-flow/ on pre-populating the data into an e-mail address field without a display control for TOTP.

     ![SignUp](https://github.com/gowthamece/Azure_B2C_Samples/assets/18691366/aa11e716-fb61-464c-a9e8-cd56a5719dd6)

 The email address is pre-populated in the email verification display control based on the query string value 

  3. Defined the email claim type as read-only

          <ClaimType Id="email">
          	<DisplayName>Email Address</DisplayName>
          	<DataType>string</DataType>
          	<UserInputType>Readonly</UserInputType>
         </ClaimType> 

