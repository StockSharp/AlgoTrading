# Estratégia EURUSD V2.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de reversão à média para EURUSD utilizando uma média móvel simples (SMA) de longo prazo e filtro de volatilidade baseado no Average True Range (ATR).

## Lógica da estratégia

- Calcular uma SMA de comprimento *MA Length* no tipo de candle escolhido.
- Entrar **vendido** quando o preço está acima da SMA e recua dentro de *Buffer* pips enquanto o ATR está abaixo de *ATR Threshold*.
- Entrar **comprado** quando o preço está abaixo da SMA e se aproxima dentro de *Buffer* pips com ATR baixo.
- O tamanho da posição é derivado do saldo da conta e do *Risk Factor Z*.
- Stop-loss e take-profit são colocados a distâncias fixas em pips a partir do preço de entrada.
- Após a saída, o sistema aguarda o preço se afastar *Noise Filter* pips do nível de entrada antes de permitir uma nova operação.

## Parâmetros

- **MA Length** – período da média móvel simples (padrão 218).
- **Buffer (pips)** – distância máxima da SMA para acionar a entrada (padrão 0).
- **Stop Loss (pips)** – distância do stop-loss da entrada (padrão 20).
- **Take Profit (pips)** – distância do take-profit da entrada (padrão 350).
- **Noise Filter (pips)** – distância para redefinir a permissão de trading (padrão 50).
- **ATR Length** – período de cálculo do ATR (padrão 200).
- **ATR Threshold (pips)** – ATR máximo para permitir novas posições (padrão 40).
- **Max Spread (pips)** – spread máximo permitido (padrão 4).
- **Risk Factor Z** – fator de gestão monetária (padrão 2).
- **Candle Type** – período dos candles processados (padrão 15 minutos).

Esta estratégia usa ordens de mercado para entradas e saídas.
