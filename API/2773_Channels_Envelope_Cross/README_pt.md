# Estratégia de Cruzamento de Canais com Envelope
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia de Cruzamento de Canais com Envelope é um port direto do consultor especialista MetaTrader "Channels". O sistema opera velas horárias e monitora uma média móvel exponencial (EMA) rápida de dois períodos em relação a três envelopes baseados em EMA (desvios de 0.3%, 0.7% e 1.0%) calculados a partir de uma EMA lenta de 220 períodos. Rompimentos da EMA rápida através desses envelopes geram entradas direcionais, enquanto um filtro de tempo opcional restringe o trading a horas específicas.

## Lógica de trading

1. **Pilha de indicadores**
   - EMA rápida (comprimento 2) calculada sobre preços de fechamento de vela.
   - EMA rápida (comprimento 2) calculada sobre preços de abertura de vela.
   - EMA lenta (comprimento 220) calculada sobre preços de fechamento de vela.
   - Três níveis de envelope derivados da EMA lenta com desvios de 0.3%, 0.7% e 1.0%.
2. **Configuração comprada**
   - Ativada quando a EMA rápida de fechamento cruza acima do envelope inferior de 1.0% ou 0.7%, permanece abaixo do envelope inferior de 0.3% por duas barras consecutivas, cruza acima da EMA lenta, ou rompe os envelopes superiores de 0.3% ou 0.7%. Qualquer uma dessas condições pode disparar uma entrada comprada quando não há posição aberta.
3. **Configuração vendida**
   - Ativada quando a EMA rápida de abertura cruza abaixo de qualquer um dos envelopes superiores, cai abaixo da EMA lenta, ou perfura os envelopes inferiores de cima. Qualquer uma dessas condições pode disparar uma entrada vendida quando não há posição aberta.
4. **Gerenciamento de risco**
   - Níveis fixos de stop-loss e take-profit (por lado) são expressos em pips e convertidos para distância de preço usando o tamanho de tick do instrumento. Se as entradas forem definidas como zero, o nível respectivo não é aplicado.
   - Trailing stops independentes para posições compradas e vendidas movem o stop de proteção para mais perto do preço de mercado quando o lucro excede a distância de trailing mais um incremento de passo configurável.
5. **Filtro de tempo**
   - Quando habilitado, a estratégia processa entradas apenas durante o intervalo de horas inclusivo configurado. As posições ainda são gerenciadas quando o filtro está ativo.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `OrderVolume` | Tamanho da ordem usado para entradas de mercado (lotes ou contratos dependendo do instrumento). |
| `UseTradeHours` | Habilita o filtro de tempo para entradas. |
| `FromHour` / `ToHour` | Horas de início e fim inclusivas para a janela de trading (suporta intervalos noturnos). |
| `StopLossBuyPips` / `StopLossSellPips` | Distância do stop-loss para operações compradas/vendidas expressa em pips. |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Distância do take-profit para operações compradas/vendidas expressa em pips. |
| `TrailingStopBuyPips` / `TrailingStopSellPips` | Distância do trailing stop em pips para operações compradas/vendidas. |
| `TrailingStepPips` | Incremento mínimo (em pips) necessário para mover um trailing stop. |
| `CandleType` | Série de velas usada para cálculos (padrão é período de 1 hora). |

## Gerenciamento de posições

- Na entrada, a estratégia armazena o preço de execução, calcula os alvos de stop-loss e take-profit em unidades de preço absoluto e redefine os níveis de trailing.
- Enquanto uma posição comprada estiver aberta, o stop-loss é seguido para cima sempre que o lucro exceder `TrailingStopBuyPips + TrailingStepPips`. A estratégia sai no stop-loss ou take-profit, o que for atingido primeiro.
- Enquanto uma posição vendida estiver aberta, o stop-loss é seguido para baixo usando os parâmetros de trailing do lado vendido e as saídas são executadas simetricamente.

## Notas

- O tamanho do pip é derivado do tamanho do tick do instrumento. Para instrumentos de três ou cinco decimais, o pip é multiplicado por dez para emular a lógica do MetaTrader.
- A estratégia trabalha com uma única posição por vez. Uma nova entrada só é colocada depois que a posição existente foi fechada.
- Habilite `StartProtection` na classe base para proteção contra posições abertas inesperadas após reinicializações (já chamado na implementação).
