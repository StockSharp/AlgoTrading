# Estratégia Exp AFIRMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia Exp AFIRMA** reproduz o consultor especialista MetaTrader `Exp_AFIRMA.mq5` usando a API de alto nível do
StockSharp. O sistema original baseia-se no indicador AFIRMA (Adaptive Finite Impulse Response Moving Average) que combina
um suavizador FIR com janela e uma previsão ARMA curta. A versão StockSharp mantém a mesma lógica de mercado: abre posições
compradas quando o componente ARMA gira para cima e fecha ou inverte quando a previsão cai para o lado baixista.

As decisões de trading são tomadas em candles completadas de um período configurável (padrão: H4). A estratégia avalia
valores ARMA de várias barras fechadas para confirmar uma mudança de inclinação. As ordens são colocadas a mercado com
stops e objetivos de proteção opcionais implementados através do gerenciamento de risco do StockSharp.

## Lógica de Trading

1. **Cálculo do indicador**
   - O `AfirmaIndicator` embutido recria o filtro AFIRMA de dois estágios. Um suavizador FIR com janela (comprimento =
     `Taps`, largura de banda = `Periods`) produz uma média móvel base.
   - A previsão ARMA é calculada através dos mesmos coeficientes de mínimos quadrados do código fonte MQL. O indicador
     expõe valores FIR e ARMA; a estratégia consome apenas o componente ARMA.
2. **Avaliação de sinais**
   - Em cada candle completada o valor ARMA mais recente é armazenado. O parâmetro `SignalBar` (padrão: 1) especifica
     quantas barras já fechadas devem ser ignoradas.
   - **Setup altista**: o valor ARMA anterior é menor que seu antecessor (`ARMA[t-2] < ARMA[t-3]`) e o valor mais recente
     está acima do anterior (`ARMA[t-1] > ARMA[t-2]`). Isso fecha a exposição vendida e abre/estende uma posição comprada
     se permitido.
   - **Setup baixista**: o valor ARMA anterior é maior que seu antecessor enquanto o valor mais recente está abaixo. Isso
     fecha a exposição comprada e abre/estende uma posição vendida se permitido.
3. **Gerenciamento de posições**
   - Apenas uma posição é mantida. Novas entradas levam a posição em direção a `±TradeVolume`. A exposição existente é
     fechada antes de inverter.
   - A proteção de risco opcional usa `StartProtection` com distâncias de stop-loss e take-profit baseadas em preço.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `TradeVolume` | Tamanho de posição base usado para entradas compradas e vendidas. |
| `CandleType` | Período/tipo de dados solicitado do adaptador de dados de mercado (padrão: candles de 4 horas). |
| `Periods` | Largura de banda recíproca do estágio FIR (`1 / (2 * Periods)`), idêntico à entrada do EA original. |
| `Taps` | Número de coeficientes FIR. Ajustado internamente ao valor ímpar mais próximo se necessário. |
| `Window` | Função de janela aplicada ao filtro FIR (`Rectangular`, `Hanning1`, `Hanning2`, `Blackman`, `BlackmanHarris`). |
| `SignalBar` | Número de barras já fechadas para olhar para trás em busca de confirmação. `1` corresponde à última barra completamente fechada. |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir abertura de posições compradas/vendidas. |
| `EnableBuyExits` / `EnableSellExits` | Permitir fechamento de posições compradas/vendidas em sinais opostos. |
| `StopLossPoints` | Stop de proteção opcional expresso em unidades de preço. |
| `TakeProfitPoints` | Objetivo de proteção opcional expresso em unidades de preço. |

## Notas de Conversão

- As opções de gerenciamento de dinheiro (`MM`, `MMMode`, `Deviation_`) da versão MetaTrader são substituídas pelo
  parâmetro mais simples `TradeVolume`.
- O EA original envia valores de stop-loss e take-profit em pontos. Aqui eles são fornecidos em unidades de preço
  absolutas. Converta pontos para preço multiplicando pelo passo de preço apropriado.
- Quando `SignalBar = 1`, a estratégia lê os últimos três valores ARMA **completados** e abre ordens na próxima barra.
  Definir `SignalBar = 0` ainda funciona mas usa a barra mais recentemente fechada.
- A implementação do indicador AFIRMA corresponde à matemática original, incluindo os tipos de janela e fórmulas de
  coeficientes suportados.

## Dicas de Uso

1. Conecte a estratégia a um instrumento e portfólio, configure `TradeVolume` e selecione o período através de `CandleType`.
2. Habilite ou desabilite as direções comprada/vendida de acordo com seu plano de trading.
3. Defina `StopLossPoints` e `TakeProfitPoints` se quiser gerenciamento de risco automatizado; caso contrário, deixe-os
   em zero para operar sem saídas fixas.
4. Monitore o gráfico gerado para verificar as linhas AFIRMA e as operações executadas ao ajustar `Periods`, `Taps` e
   `SignalBar`.
