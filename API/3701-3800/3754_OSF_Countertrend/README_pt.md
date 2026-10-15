# Estratégia Contratendência da OSF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz o especialista em contratendência Open Source Forex "Overbought/Oversold".
Ele se aproxima do oscilador original calculando a média de várias RSI leituras e interpretações
a distância do nível de equilíbrio (50) como sinal de direção e tamanho de posição.
As negociações são executadas em velas finalizadas e fechadas por um take-profit fixo medido em
pontos do instrumento.

## Regras de negociação

- **Dados**: Candles finalizadas do `CandleType` configurado.
- **Indicador**: RSI com período definido por `RsiPeriod`. O especialista MQL original teve média de cinco
valores RSI idênticos, portanto, um único RSI é suficiente aqui.
- **Lógica de sinal**:
  - Quando RSI > 50, o mercado é considerado sobrecomprado e uma posição curta é aberta.
  - Quando RSI < 50, o mercado é considerado sobrevendido e uma posição longa é aberta.
  - A distância absoluta |RSI − 50| determina o volume negociado por meio de `VolumePerPoint`.
- **Cooldown**: Após cada negociação, a estratégia espera por `CooldownBars` velas concluídas antes
avaliando uma nova entrada. Isso imita o comportamento de suavização de barra do código-fonte.
- **Saídas**: cada entrada coloca um take-profit manual a `TakeProfitPoints` * `PriceStep` de distância de
o preço de preenchimento. Nenhum stop loss é usado, exatamente como no especialista original.
- **Reversões**: Abrir uma negociação na direção oposta fecha primeiro qualquer posição existente
ajustando o volume da ordem de mercado.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `RsiPeriod` | Comprimento RSI usado para aproximar o oscilador OSF (padrão 14). |
| `VolumePerPoint` | Volume negociado para cada RSI ponto fora do nível 50 (padrão 0,01). |
| `TakeProfitPoints` | Distância até a meta de lucro expressa em pontos de instrumento (padrão 150). |
| `CooldownBars` | Número de velas concluídas a serem ignoradas após cada negociação (padrão 5). |
| `CandleType` | Tipo de vela para cálculos de indicadores (período padrão de 1 minuto). |

## Notas

- A estratégia assume que `PriceStep` está definido para o instrumento selecionado; caso contrário, uma unidade
a etapa 1 é usada para calcular o nível de lucro.
- Como o especialista original não tinha stop-loss de proteção, o gerenciamento de risco deveria ser adicionado
manualmente ao implantar a estratégia ao vivo.
