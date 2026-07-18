# Estratégia de Bollinger Divergência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Bollinger Divergência busca extremos onde o preço perfura uma banda enquanto a
banda oposta começa a se contrair. Essa divergência entre o impulso do preço
e a volatilidade frequentemente precede um retorno ao centro do intervalo.

Um sinal de compra aparece quando uma vela fecha abaixo da banda inferior enquanto
a banda superior se estreita pelo menos um percentual definido. Para vendas, o
padrão é espelhado em torno da banda superior. As posições visam um movimento rápido
de volta à linha central de Bollinger com uma tomada de lucro fixa opcional.

O setup funciona melhor em mercados em lateralização ou após um pico de volatilidade
começar a dissipar. O parâmetro `CandlePercent` controla o quanto a banda oposta
deve se contrair antes de permitir uma operação, ajudando a evitar sinais falsos
durante tendências fortes.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: Fechamento abaixo da banda inferior E a banda superior se contrai em `CandlePercent`.
  - **Vendido**: Fechamento acima da banda superior E a banda inferior se contrai em `CandlePercent`.
- **Critérios de saída**:
  - Retorno à banda central OU percentual de tomada de lucro.
- **Stops**: Sem stop fixo; depende da tomada de lucro ou saída manual.
- **Valores padrão**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `TakeProfit` = 5
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado/Vendido
  - Indicadores: Bollinger Bands
  - Complexidade: Simples
  - Nível de risco: Médio
