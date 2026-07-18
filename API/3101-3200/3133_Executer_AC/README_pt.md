# Estratégia de Executer AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Executer AC** é um port fiel do StockSharp do assessor especialista MetaTrader 5 "Executer AC". O EA original negocia no **Accelerator Oscillator (AC)** desenvolvido por Bill Williams e combina suas oscilações de momentum com um framework fixo de stop/limite e um módulo de trailing stop. Esta conversão mantém o comportamento da versão MQL5 enquanto expõe parâmetros fáceis de usar que se integram com a API de alto nível do StockSharp.

## Lógica de negociação

A estratégia opera com velas finalizadas do período selecionado e depende dos últimos quatro valores do Accelerator Oscillator:

- `AC[0]` – barra completada mais recente (chamada de `ac[1]` no código original).
- `AC[1]`, `AC[2]`, `AC[3]` – valores progressivamente mais antigos usados para detecção de padrões.

A árvore de decisão é idêntica ao EA:

1. **Gestão de posição**
   - Posições longas são fechadas quando `AC[0] < AC[1]` (momentum decrescente).
   - Posições curtas são fechadas quando `AC[0] > AC[1]` (momentum crescente).
   - Uma rotina de trailing stop aperta dinamicamente o stop protetor assim que o preço avança além da distância configurada mais o passo de trailing.
2. **Regras de entrada quando flat**
   - **Aceleração altista acima de zero:** se `AC[0] > 0` e `AC[1] > 0` e `AC[0] > AC[1] > AC[2]`, uma compra de mercado é emitida.
   - **Aceleração baixista acima de zero:** se `AC[0] > 0` e `AC[1] > 0` e `AC[0] < AC[1] < AC[2] < AC[3]`, uma venda de mercado é emitida.
   - **Aceleração altista abaixo de zero:** se `AC[0] < 0` e `AC[1] < 0` e `AC[0] > AC[1] > AC[2] > AC[3]`, uma compra de mercado é emitida.
   - **Aceleração baixista abaixo de zero:** se `AC[0] < 0` e `AC[1] < 0` e `AC[0] < AC[1] < AC[2]`, uma venda de mercado é emitida.
   - **Cruzamentos da linha zero:** um cruzamento descendente (`AC[0] > 0` e `AC[1] < 0`) aciona uma compra; um cruzamento ascendente (`AC[0] < 0` e `AC[1] > 0`) aciona uma venda.

Os sinais são avaliados apenas após confirmar que as velas estão finalizadas, os valores do indicador estão formados e a negociação está habilitada.

## Gestão de risco

- **Stop-loss e take-profit:** distâncias configuráveis (em pips) convertidas em unidades de preço usando o step do instrumento. Os stops são recalculados a cada nova entrada e permanecem inalterados até serem atingidos ou substituídos pelo trailing stop.
- **Trailing stop:** replica a lógica do EA. Quando o lucro não realizado excede `TrailingStop + TrailingStep` (ambos em pips), o preço do stop é movido para `Close - TrailingStop` para posições longas e `Close + TrailingStop` para posições curtas, exigindo a melhora requerida antes de cada passo.
- **Proteção de posição:** o helper integrado `StartProtection()` é invocado para que o StockSharp proteja contra desconexões inesperadas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|------------|
| `TradeVolume` | Volume de ordem usado para todas as entradas de mercado. Normalizado de acordo com o step de volume e limites do instrumento. |
| `StopLossPips` | Distância do stop-loss em pips. Um valor de zero desabilita o stop-loss. |
| `TakeProfitPips` | Distância do take-profit em pips. Um valor de zero desabilita o take-profit. |
| `TrailingStopPips` | Distância do trailing stop em pips. Defina como zero para desabilitar o trailing. |
| `TrailingStepPips` | Lucro adicional mínimo (em pips) necessário antes de mover o trailing stop novamente. |
| `CandleType` | Período das velas usadas para calcular o Accelerator Oscillator. |

## Notas de implementação

- A normalização de preços respeita tanto o tamanho do tick da bolsa quanto símbolos Forex de três/cinco dígitos, multiplicando o tamanho do ponto por dez quando apropriado.
- O histórico do indicador é mantido em um buffer de tamanho fixo para replicar as comparações originais `ac[1] … ac[4]` sem recorrer a coleções custosas ou consultas de histórico.
- A estratégia sempre sai antes de avaliar novas entradas na mesma vela, correspondendo ao fluxo de controle do EA MQL5 onde instruções `return` evitam re-entrada imediata.
- Os valores do trailing stop atualizam tanto o estado interno de trailing quanto o preço efetivo do stop usado para verificações de stop-loss, garantindo consistência com o comportamento `PositionModify` do EA.

## Dicas de uso

1. Escolha um período de vela que corresponda ao mercado negociado (o script original era comumente usado em gráficos Forex intradía).
2. Ajuste as distâncias de stop-loss, take-profit e trailing à volatilidade do instrumento escolhido; valores extremamente apertados podem levar a whipsaws frequentes.
3. Ative controles de risco no lado do broker conectado quando possível, pois a estratégia depende de saídas pelo lado do software.
4. Combine com gestão de dinheiro em nível de portfólio se pretender executar múltiplas estratégias simultaneamente.
