# Estratégia de Lacunas (Gaps)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de price action que reage a lacunas de abertura entre velas consecutivas. Ela espera que uma nova barra abra além
 da máxima ou mínima anterior por uma distância configurável em pips, entra na direção da reversão esperada e gerencia a operação
 com stops fixos, alvos e um trailing stop escalonado opcional.

## Como funciona

1. A estratégia monitora um único símbolo usando o período selecionado.
2. Quando uma nova vela é formada, ela compara o preço de abertura com a vela anterior:
   - Se a abertura estiver abaixo da mínima anterior menos `GapPips`, a estratégia entra em uma posição comprada esperando um recuo altista.
   - Se a abertura estiver acima da máxima anterior mais `GapPips`, ela entra em uma posição vendida antecipando uma correção descendente.
3. Dentro de uma operação, o gerenciamento de risco é tratado inteiramente dentro da estratégia:
   - Um stop-loss fixo é colocado a `StopLossPips` abaixo (para comprado) ou acima (para vendido) do preço de entrada.
   - Um take-profit fixo é definido a `TakeProfitPips` do preço de entrada na direção da operação.
   - Um trailing stop pode ser ativado; ele só se move depois que o preço avançou `TrailingStopPips + TrailingStepPips` e então
     bloqueia lucros mantendo o stop a `TrailingStopPips` do preço mais favorável.
4. Os níveis de proteção são avaliados em cada vela concluída usando os extremos de máxima/mínima para que toques intrabarra disparem saídas de forma confiável.
5. Ordens abertas são canceladas antes de tomar uma nova posição, e reversões de posição fecham automaticamente o lado oposto.

## Parâmetros

- `OrderVolume` = 0.1 — volume de trading em lotes para cada nova posição.
- `StopLossPips` = 50 — distância do preço de entrada ao nível de stop-loss em pips. Definir como 0 para desabilitar o stop.
- `TakeProfitPips` = 50 — distância do preço de entrada ao nível de take-profit em pips. Definir como 0 para desabilitar o alvo.
- `TrailingStopPips` = 5 — tamanho do trailing stop em pips. Definir como 0 para desativar o trailing.
- `TrailingStepPips` = 5 — melhoria mínima de preço (em pips) necessária antes que o trailing stop se mova novamente.
- `GapPips` = 1 — lacuna de abertura mínima, expressa em pips, necessária para gerar um sinal de entrada.
- `CandleType` = período de 1 hora — velas usadas para detecção de lacunas e gerenciamento de posições.

## Notas de implementação

- Entradas baseadas em pips são convertidas para distâncias de preço absolutas usando o tamanho de tick do instrumento. Cotações
  forex de cinco e três dígitos são ajustadas automaticamente para trabalhar com valores reais de pip.
- A lógica de trailing stop requer que `TrailingStepPips` seja positivo quando `TrailingStopPips` está habilitado; caso contrário, a estratégia lança
  uma exceção na inicialização, espelhando a validação MQL original.
- A estratégia avalia controles de risco apenas em velas terminadas de acordo com as diretrizes da API de alto nível do StockSharp.
- O gerenciamento manual de stop e alvo depende de ordens de mercado, portanto não há ordens de proteção separadas no livro.
- As configurações padrão assumem instrumentos forex; ajuste as distâncias em pips ao negociar ativos com volatilidade ou tamanhos de tick diferentes.
