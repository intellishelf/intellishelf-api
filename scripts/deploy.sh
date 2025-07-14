#!/bin/bash

# Build and publish
dotnet publish Intellishelf.Api/Intellishelf.Api.csproj -c Release -o publish

# Create deployment package
cd publish && zip -r ../deployment.zip . && cd ..

# Deploy to Azure
az webapp deploy --resource-group intellitest --name intellishelf-test --src-path deployment.zip 