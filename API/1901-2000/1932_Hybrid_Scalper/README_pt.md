# Estratégia Hybrid Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Hybrid Scalper** é um algoritmo de trading de curto prazo convertido do script MQL4 `hybrid_Scalper.mq4`. Opera sobre a API de alto nível do StockSharp e é projetada para o período de 1 minuto. A estratégia combina múltiplos indicadores técnicos para identificar oportunidades de rompimento rápido, evitando períodos de volatilidade excessiva ou insuficiente.

## Lógica da estratégia

1. **Filtro de tendência** – Uma EMA rápida (21) e uma EMA lenta (89) determinam a direção do mercado. Operações compradas são permitidas apenas quando a EMA rápida está acima da EMA lenta; operações vendidas requerem a condição oposta.
2. **Filtro de momentum** – O Oscilador Estocástico (5,3,3) gera sinais de entrada. Uma compra é acionada quando %K está abaixo de 20 e abaixo de %D. Uma venda é acionada quando %K está acima de 80 e ainda acima de %D.
3. **Confirmação RSI** – O Índice de Força Relativa com período 7 confirma o momentum. Entradas compradas requerem RSI abaixo de 25, enquanto entradas vendidas requerem RSI acima de 85.
4. **Filtro de volatilidade** – As Bandas de Bollinger (50, desvio 4) medem a largura atual do mercado. A estratégia opera apenas quando a diferença entre as bandas superior e inferior está entre 0.00045 e 0.00262, evitando mercados tanto calmos quanto instáveis.
5. **Dias de trading** – Os parâmetros permitem habilitar ou desabilitar o trading para cada dia da semana individualmente (segunda–sexta).

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `RsiPeriod` | Período do indicador RSI. |
| `EmaFastPeriod` | Período da EMA rápida para detecção de tendência. |
| `EmaSlowPeriod` | Período da EMA lenta para detecção de tendência. |
| `BbPeriod` | Período usado nas Bandas de Bollinger. |
| `BbDeviation` | Multiplicador de desvio para as Bandas de Bollinger. |
| `TradeMonday`–`TradeFriday` | Habilitar trading em dias da semana específicos. |
| `CandleType` | Tipo de vela/período, padrão são velas de 1 minuto. |

## Notas

- A estratégia usa a API de alto nível `BindEx` para conectar múltiplos indicadores em uma única assinatura.
- `StartProtection()` é invocado uma vez no início para ativar a proteção de posição integrada (sem parâmetros explícitos de stop-loss ou take-profit).
- Todos os comentários no código são fornecidos em inglês de acordo com as diretrizes do repositório.

## Como executar

1. Adicione o arquivo de estratégia a um projeto StockSharp.
2. Configure os conectores de dados de mercado e execução necessários.
3. Compile e inicie a estratégia; certifique-se de que o instrumento selecionado forneça velas de 1 minuto.
4. Ajuste os parâmetros através da interface `StrategyParam` conforme necessário.
