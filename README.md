# Azure_B2C_Samples
The repository contains a custom policies extension file which are used to handle different real time use cases.

**SignUpOrSignIn_SplitEmailVerificationAndSignUp** custom policy extension file is used to split the email verification step from the sign up user journey on top of that I have integrated a REST API call (Azure function)
to pre-populate the Display Name, First Name and Last Name fields in the sign up page.


## Azure AD B2C Web Applications 
Created two web applications 1.[Weather App](https://github.com/gowthamece/Azure_B2C_Samples/tree/main/Web%20App/B2CWebApps/WeatherAppMVC) and 2. [To-Do-List app](https://github.com/gowthamece/Azure_B2C_Samples/tree/main/Web%20App/B2CWebApps/ToDoListAppMVC) to demonstrate [Secure Single sign-out](https://gowthamoncloud.com/secure-single-sign-out-in-azure-ad-b2c/).  

