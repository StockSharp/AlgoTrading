# Estratégia Simples de Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Simples de Alligator recria o expert advisor do MetaTrader "Alligator Simple v1.0" usando a API de alto nível do StockSharp. Ela lê o indicador Alligator de Bill Williams em velas terminadas e abre uma posição quando as linhas Lips, Teeth e Jaw se expandem na mesma direção na barra completada anterior. Cada trade pode incluir opcionalmente gestão de stop-loss, take-profit e trailing stop baseados em pips que reflete a implementação MQL original.

## Indicadores e dados
- **Linhas Alligator**: três Médias Móveis Suavizadas (SMMA) calculadas sobre o preço médio da vela `(high + low) / 2` com comprimentos configuráveis e deslocamentos para frente para o Jaw, Teeth e Lips.
- **Velas**: a estratégia assina um único `CandleType` configurável (velas de uma hora por padrão) e apenas processa velas terminadas para evitar viés de antecipação.

## Lógica de trading
1. **Avaliação de sinais**
   - Recuperar os valores deslocados do Alligator para a vela completada anterior.
   - Sinal comprado: `Lips[t-1] > Teeth[t-1] > Jaw[t-1]`.
   - Sinal vendido: `Lips[t-1] < Teeth[t-1] < Jaw[t-1]`.
2. **Execução**
   - Entrar no mercado com `OrderVolume` quando não há posição aberta.
   - Apenas uma posição é mantida por vez; sinais opostos são ignorados até que a posição atual seja fechada.

## Saída e gestão de risco
- **Stop-loss inicial**: se `StopLossPips > 0`, a estratégia desloca o preço de execução pela distância em pips convertida com o passo de preço do instrumento (incluindo o multiplicador de pips de 3/5 dígitos usado por símbolos MetaTrader).
- **Take-profit**: quando `TakeProfitPips > 0`, um alvo de lucro é colocado simetricamente ao redor do preço de entrada. Um valor zero desativa o alvo.
- **Trailing stop**: quando tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos, o stop avança para `close − TrailingStop` (comprados) ou `close + TrailingStop` (vendidos) uma vez que o preço se moveu pelo menos `TrailingStop + TrailingStep` a favor do trade. As atualizações de trailing dependem da máxima/mínima da vela para simular toques intra-barra.
- **Tratamento de saída**: condições de stop-loss, take-profit e trailing emitem ordens a mercado para zerar a posição e são avaliadas a cada vela terminada.

## Parâmetros
- `OrderVolume` (padrão **1**): tamanho do trade em lotes ou contratos.
- `StopLossPips` (padrão **100**): distância do stop-loss inicial em pips. Definir como zero para desabilitar.
- `TakeProfitPips` (padrão **100**): distância do take-profit em pips. Definir como zero para desabilitar.
- `TrailingStopPips` (padrão **5**): distância do trailing stop em pips. Zero desativa o trailing.
- `TrailingStepPips` (padrão **5**): distância extra em pips que o preço deve percorrer antes que o trailing stop avance. Deve ser positivo quando o trailing está habilitado.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: comprimentos SMMA para o jaw, teeth e lips (padrões 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: deslocamentos para frente aplicados ao ler os valores do Alligator (padrões 8/5/3).
- `CandleType`: tipo/período de dados de velas para cálculos (padrão velas de uma hora).

## Notas de implementação
- As distâncias em pips se adaptam automaticamente ao tamanho do tick do ativo. Instrumentos com três ou cinco casas decimais multiplicam o passo de preço por dez para replicar a definição de pip do MetaTrader.
- Os buffers de histórico do indicador armazenam valores suficientes para respeitar os deslocamentos para frente configurados, eliminando a manipulação manual de arrays.
- A estratégia usa os helpers `BuyMarket` e `SellMarket` para enviar ordens, mantendo o código focado na geração de sinais e no tratamento de risco.
