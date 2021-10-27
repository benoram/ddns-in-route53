#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1-focal AS base

RUN apt-get update && apt-get -y upgrade

COPY ["artifacts", "."]

ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "ddns-in-route53.dll"]
