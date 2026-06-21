# Estratégia BnB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do Expert Advisor do MetaTrader 5 "Exp_BnB". Ela utiliza o indicador personalizado BnB (Bulls and Bears) que mede a pressão de alta e baixa dentro de cada candle e as suaviza com uma média móvel exponencial.

## Como funciona

1. Para cada candle finalizado, a estratégia calcula os valores de bulls e bears.
2. Ambas as séries são suavizadas com EMA.
3. Quando a linha bulls cruza acima da linha bears:
   - Qualquer posição vendida é fechada.
   - Uma posição comprada é aberta.
4. Quando a linha bears cruza acima da linha bulls:
   - Qualquer posição comprada é fechada.
   - Uma posição vendida é aberta.
5. Níveis de stop loss e take profit são gerenciados em pontos de preço absolutos.

## Parâmetros

- `Candle Type` – período dos candles usados para os cálculos.
- `EMA Length` – período de suavização para bulls e bears.
- `Stop Loss` – distância até o stop de proteção em pontos de preço.
- `Take Profit` – distância até o alvo de lucro em pontos de preço.
- `Allow Long Entry` – habilitar abertura de posições compradas.
- `Allow Short Entry` – habilitar abertura de posições vendidas.
- `Allow Long Exit` – habilitar fechamento de posições compradas.
- `Allow Short Exit` – habilitar fechamento de posições vendidas.

## Observações

O indicador original suporta múltiplos métodos de suavização. Neste port, o filtro universal é aproximado com uma média móvel exponencial padrão.
