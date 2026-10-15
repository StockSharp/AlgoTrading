# Estratégia de Rompimento Aeron JJN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica do consultor especialista original Aeron JJN. Ela observa uma vela de reversão forte e coloca uma ordem stop na abertura da última vela oposta. O stop e o alvo são definidos a um ATR de distância, e um trailing stop opcional protege as posições abertas.

Os testes mostram que a ideia funciona melhor em pares Forex principais usando velas de 1 minuto.

Uma ordem buy stop é colocada quando a vela anterior é baixista com corpo maior que **DojiDiff1** e a vela atual é altista, mas ainda abaixo da última abertura baixista significativa. Uma ordem sell stop usa as condições espelho. Ordens pendentes são removidas após **ResetTime** minutos se permanecerem não executadas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Vela anterior baixista, vela atual altista e fecha abaixo da última abertura baixista.
  - **Vendido**: Vela anterior altista, vela atual baixista e fecha acima da última abertura altista.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop-loss e take-profit baseados em ATR.
  - Trailing stop opcional em pips.
- **Stops**: Sim, stop inicial e alvo baseados em ATR mais trailing opcional.
- **Filtros**:
  - Ordens pendentes expiram após o tempo configurado.

## Parâmetros

- `AtrPeriod` – período de cálculo do ATR.
- `DojiDiff1` – limiar de tamanho do corpo para a vela anterior.
- `DojiDiff2` – limiar de tamanho do corpo ao buscar a última vela oposta.
- `TrailSl` – ativar trailing stop.
- `TrailPips` – distância de trailing em pips.
- `ResetTime` – minutos antes de cancelar ordens stop.
- `CandleType` – período de trabalho.
