# Azure_B2C_Samples
The repository contains a custom policies extension file which are used to handle different real time use cases.

SignUpOrSignIn_SplitEmailVerificationAndSignUp custom policy extension file is used to split the email verification step from the sign up user journey on top of that I have integrated a REST API call (Azure function)
to pre-populate the Display Name, First Name and Last Name fields in the sign up page.
