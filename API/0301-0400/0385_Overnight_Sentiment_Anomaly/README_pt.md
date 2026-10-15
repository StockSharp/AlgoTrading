# Estratégia de Anomalia de Sentimento Noturno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia um ETF de ações apenas durante a noite quando um indicador de sentimento externo sinaliza otimismo extremo. No fechamento, o ETF é comprado se o indicador ultrapassar um limiar e é vendido na manhã seguinte, visando a deriva noturna associada ao sentimento positivo.

Dados intradiários não são utilizados; o algoritmo reage a valores de sentimento no final do dia e coloca ordens de mercado no fechamento e na abertura do dia seguinte.

## Detalhes

- **Instrumento**: ETF de ações e série de dados de sentimento.
- **Sinal**: valor de sentimento acima do `Threshold` configurável.
- **Período de manutenção**: fechamento do mercado até a abertura do dia seguinte.
- **Posicionamento**: comprado quando o sentimento é alto, caso contrário sem posição.
- **Controle de risco**: ordem ignorada quando o valor da negociação estiver abaixo de `MinTradeUsd`.
