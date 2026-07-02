# ADX e estratégia de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão StockSharp do MetaTrader especialista **ADX_MA (fortrader)**.
Ele combina um filtro de média móvel suavizada (SMMA) com o Índice Direcional Médio (ADX)
de modo que as negociações sejam realizadas apenas quando a tendência for confirmada por um cruzamento e forte o suficiente
de acordo com ADX. A porta mantém o gerenciamento de risco assimétrico do robô original:
as posições longas utilizam amplas distâncias de lucro e trailing, enquanto as negociações curtas empregam distâncias mais apertadas.
alvos e proteção.

## Lógica de negociação

1. Construa uma média móvel suavizada sobre os preços médios das velas e um ADX com os períodos configurados.
2. Avalie sinais em velas fechadas apenas para imitar a lógica MQL4 (`iClose(...,1)` / `iClose(...,2)`).
3. Insira comprado quando a vela anterior fechar acima do SMMA, a vela antes de fechar abaixo do
mesmo valor SMMA e a leitura anterior de ADX está acima do limite.
4. Entre em posição curta quando a vela anterior fechar abaixo do SMMA, a vela antes de fechar acima do
mesmo valor SMMA e ADX está acima do limite.
5. Uma vez em posição, as saídas são conduzidas por:
   - Inversão da média móvel na direção oposta.
   - Níveis individuais de stop-loss e take-profit medidos em pips.
   - Distâncias opcionais de trailing stop que favorecem a negociação.

Todas as compensações de preço são convertidas de pips usando a etapa de preço do título. Se o instrumento não
relatar uma etapa válida, um valor de 1 será usado como um substituto seguro.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `SMMA Period` | Comprimento da média móvel suavizada (padrão 21). |
| `ADX Period` | Comprimento do Índice Direcional Médio (padrão 14). |
| `ADX Threshold` | Valor mínimo de ADX necessário para permitir entradas (padrão 16). |
| `Long Take Profit (pips)` | Distância de lucro para posições de compra (padrão 1300 pips). |
| `Long Stop Loss (pips)` | Distância de stop loss para posições de compra (padrão 30 pips). |
| `Long Trailing Stop (pips)` | Distância do trailing-stop para posições de compra (padrão 270 pips). |
| `Short Take Profit (pips)` | Distância de lucro para posições de venda (padrão 160 pips). |
| `Short Stop Loss (pips)` | Distância de stop-loss para posições de venda (padrão 50 pips). |
| `Short Trailing Stop (pips)` | Distância do trailing-stop para posições de venda (padrão 20 pips). |
| `Volume` | Volume do pedido usado para novas entradas (padrão 0,1). |
| `Candle Type` | Série de velas primárias para cálculos (período padrão de 1 minuto). |

Todos os parâmetros são expostos para otimização. Os padrões correspondem às configurações originais do EA.

## Notas

- Os trailing stops são ativados somente depois que o preço se move pelo menos a distância configurada a partir da entrada.
- Os sinais opostos fecham a posição ativa antes de abrir uma nova.
- A estratégia desenha automaticamente velas, indicadores e negociações próprias no gráfico se uma área do gráfico estiver disponível.
- Não há testes automatizados para esta porta; use backtesting manual para validar o comportamento em seus instrumentos.
