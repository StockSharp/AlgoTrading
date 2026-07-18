# Estratégia Sidus Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Sidus reproduz a lógica clássica do assessor especializado "Sidus" do MetaTrader no StockSharp. Ela combina o indicador Alligator de Bill Williams com um filtro de Índice de Força Relativa (RSI) de 14 períodos. O sistema procura um cruzamento RSI acima ou abaixo da linha média 50 enquanto todas as três médias móveis do Alligator se expandem na mesma direção. Cada entrada calcula imediatamente stops de proteção e gestão opcional de trailing expressa em distâncias de pips que respeitam o passo de preço do instrumento.

## Indicadores e dados
- **Linhas do Alligator**: três médias móveis suavizadas calculadas sobre o preço mediano da vela (máximo + mínimo ÷ 2) com comprimentos e deslocamentos à frente independentes para mandíbula, dentes e lábios. Valores consecutivos são comparados para detectar expansão para cima ou para baixo.
- **Índice de Força Relativa (RSI)**: um oscilador de 14 períodos avaliado em preços de fechamento. Apenas velas terminadas participam da decisão para evitar viés de antecipação.
- **Velas**: qualquer período pode ser selecionado através do parâmetro `CandleType`. Por padrão, a estratégia usa velas de período de um minuto.

## Lógica de trading
1. **Confirmação RSI**
   - Configuração comprada: RSI cruza para cima por 50 (`RSI[t-2] < 50` e `RSI[t-1] > 50`).
   - Configuração vendida: RSI cruza para baixo por 50 (`RSI[t-2] > 50` e `RSI[t-1] < 50`).
2. **Filtro de inclinação do Alligator**
   - A entrada comprada requer que as inclinações da mandíbula, dentes e lábios entre os dois valores completados anteriores (levando em conta os deslocamentos) excedam o limiar `Delta`.
   - A entrada vendida requer que as mesmas inclinações estejam abaixo do limiar, indicando compressão ou declínio.
3. **Gerenciamento de posições**
   - Quando um sinal comprado aparece, as vendidas são fechadas primeiro se `CloseOpposite = true`. A estratégia então compra o `OrderVolume` configurado a mercado.
   - Quando um sinal vendido aparece, as compradas são zeradas se permitido por `CloseOpposite`, seguido de uma venda de mercado de `OrderVolume`.

## Saída e gestão de risco
- **Stop-loss inicial**: calculado a partir do extremo da vela anterior menos/mais `OffsetPips` (convertido usando o passo de preço do instrumento). Os stops são ignorados se o nível calculado invalidar a operação (por exemplo, distância não positiva).
- **Take-profit**: distância opcional definida por `TakeProfitPips`. Definir o parâmetro como zero desabilita o alvo.
- **Trailing stop**: se `TrailingStopPips` e `TrailingStepPips` forem ambos positivos, o stop avança quando o preço se move pelo menos `TrailingStopPips + TrailingStepPips` em favor da posição. O novo stop é colocado a `TrailingStopPips` da máxima mais alta (comprados) ou mínima mais baixa (vendidos) alcançada durante a barra.
- **Lógica de nivelamento**: o stop-loss, take-profit e a lógica de trailing são avaliados em cada vela terminada usando intervalos de máxima/mínima para simular toques intrabarra.

## Parâmetros
- `OrderVolume` (padrão **0.1**): tamanho do trade em lotes ou contratos.
- `OffsetPips` (padrão **3**): distância do extremo da vela anterior ao stop-loss. Zero desabilita o stop inicial.
- `TakeProfitPips` (padrão **75**): distância do take-profit. Zero desabilita o alvo.
- `TrailingStopPips` (padrão **5**): distância do trailing stop. Deve ser positivo se o trailing estiver habilitado.
- `TrailingStepPips` (padrão **15**): movimento adicional necessário antes que o trailing stop avance. Deve ser positivo quando o trailing estiver habilitado.
- `Delta` (padrão **0.00003**): diferença mínima de inclinação para cada linha do Alligator entre amostras consecutivas.
- `CloseOpposite` (padrão **false**): se `true`, posições opostas são fechadas antes de abrir um novo trade; se `false`, a estratégia espera que a posição atual se aplane naturalmente.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: comprimentos das médias móveis suavizadas para a mandíbula, dentes e lábios do Alligator (padrões 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: deslocamentos à frente (padrões 8/5/3) usados ao recuperar comparações de inclinações.
- `RsiPeriod` (padrão **14**): janela de média RSI.
- `CandleType`: tipo de dados de vela/período para assinar (padrão 1 minuto).

## Notas de implementação
- As distâncias baseadas em pips se adaptam automaticamente à precisão de preço do instrumento: instrumentos de cinco e três decimais multiplicam o passo de preço por dez para corresponder à definição de pip do MQL.
- As verificações de inclinação do Alligator dependem de valores históricos armazenados que respeitam os deslocamentos à frente configurados, evitando gerenciamento manual de arrays além de um buffer de anel mínimo.
- As ordens são executadas com os helpers de alto nível `BuyMarket` e `SellMarket`, mantendo a estratégia focada na geração de sinais enquanto o StockSharp gerencia o roteamento.
