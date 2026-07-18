# Estratégia de Força Compradora/Vendedora RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia calcula o RSI nas velas recebidas e o suaviza com uma EMA.
Deriva duas linhas, `cc` e `bb`, representando a pressão compradora e vendedora.
Uma posição comprada é aberta quando `cc` cruza acima de `bb`, enquanto uma posição vendida é aberta quando `cc` cruza abaixo de `bb`.
