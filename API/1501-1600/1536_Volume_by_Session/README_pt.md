# Estratégia de Volume por Sessão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simplificada derivada do indicador do TradingView **"Volume by Session"**. O dia de trading é dividido em quatro sessões, cada uma com seu próprio volume médio. Quando o volume atual dentro de uma sessão se desvia de sua média, a estratégia entra em operações de acordo.

## Detalhes

- **Entrada**: Volume da sessão atual acima ou abaixo de sua média móvel.
- **Saída**: Sinal oposto fecha a posição existente.
- **Comprado/Vendido**: Ambos.
- **Indicadores**: SMA.
- **Período**: Intradiário.

Esta é uma tradução educacional mínima; a visualização e as configurações extensas do script original são omitidas.
