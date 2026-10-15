# Estratégia ORB VWAP Braid Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento do intervalo de abertura com confirmação de VWAP e filtro Braid.

## Regras
- Opera entre 09:35 e 11:00 horário do exchange
- Uma operação por dia
- Comprado quando o preço fecha acima da máxima do intervalo de abertura, acima do VWAP e o filtro Braid é altista
- Vendido quando o preço fecha abaixo da mínima do intervalo de abertura, abaixo do VWAP e o filtro Braid é baixista
- Stop-loss no lado oposto do intervalo
- Take profit em duas vezes o risco limitado pelos níveis do dia anterior ou do pré-mercado

## Indicadores
- Média Móvel Ponderada por Volume (VWAP)
- Média Móvel Exponencial (3, 7, 14)
- Average True Range (14)
