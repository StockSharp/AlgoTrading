# Estratégia Dematus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Dematus replica a lógica do assessor especializado original "Dematus" do MetaTrader 5. Ela usa o oscilador DeMarker para detectar reversões de momentum e suporta piramidação com dimensionamento adaptativo de posições. A estratégia é projetada para um único instrumento e opera na série de velas definida pelo parâmetro `CandleType`.

Em cada vela terminada dois valores do DeMarker são avaliados: o valor mais recente e o valor de duas barras atrás. Um cruzamento do limiar de sobrevenda (0.3) para cima sinaliza oportunidades compradas, enquanto um cruzamento do limiar de sobrecompra (0.7) para baixo sinaliza oportunidades vendidas. Após uma entrada inicial, a estratégia pode adicionar à posição se o preço percorrer uma distância configurável a partir do último preço de entrada executado e o sinal do DeMarker disparar novamente.

## Regras de trading
- **Entrada primária:**
  - Abrir uma posição comprada quando o valor do DeMarker de duas barras atrás está abaixo de 0.3 e o valor atual sobe acima de 0.3, desde que não haja posição aberta.
  - Abrir uma posição vendida quando o valor do DeMarker de duas barras atrás está acima de 0.7 e o valor atual cai abaixo de 0.7, desde que não haja posição aberta.
- **Lógica de escalonamento:**
  - Enquanto uma posição está ativa, a estratégia lembra o preço exato do último preenchimento. Se o preço se mover contra a posição pelo menos `DistancePips` (convertido para unidades de preço) e o cruzamento correspondente do DeMarker ocorrer novamente, a estratégia submete uma ordem adicional na mesma direção.
  - O tamanho de cada ordem adicional é o volume executado anterior multiplicado por `VolumeMultiplier`, arredondado para o passo de volume do instrumento e restringido pelos limites da bolsa. Isso reflete o comportamento do coeficiente de lote do assessor especializado original.
- **Gestão de stops:**
  - Um stop-loss inicial é anexado a cada nova posição usando `StopLossPips`. O nível de stop é recalculado após cada trade de escalonamento para que a posição líquida consolidada sempre tenha um nível de proteção válido.
  - Se `TrailingStopPips` estiver habilitado, o nível de stop é ajustado quando o lucro aberto excede `TrailingStopPips + TrailingStepPips`, emulando a lógica de trailing stop da implementação MQL.
- **Proteção de patrimônio:**
  - Quando zerada, a estratégia define um piso de patrimônio virtual igual a `Balance - VirtualStopEquity`.
  - Uma vez que o patrimônio flutuante sobe pelo menos `TrailingStartEquity`, um stop de patrimônio em trailing é ativado e segue o patrimônio pico menos `TrailingEquity`.
  - Se o patrimônio da conta cair abaixo do piso virtual enquanto uma posição está aberta, todas as posições são liquidadas imediatamente.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `InitialVolume` | Tamanho de ordem base para a primeira operação. Usado novamente quando a posição está completamente fechada. |
| `DemarkerLength` | Período do indicador DeMarker. |
| `StopLossPips` | Distância do stop protetor em pips aplicada a cada entrada. Definir como zero para desabilitar o stop-loss estático. |
| `TrailingStopPips` | Distância do trailing stop em pips. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | Movimento favorável adicional (em pips) necessário antes que o trailing stop se mova. Deve ser positivo quando o trailing está ativo. |
| `DistancePips` | Distância de preço mínima (em pips) do último preenchimento antes de escalar na posição. |
| `TrailingEquity` | Distância entre o pico de patrimônio e o piso de patrimônio protetor. |
| `VirtualStopEquity` | Buffer inicial abaixo do saldo usado para calcular o piso de patrimônio virtual quando a estratégia está zerada. |
| `TrailingStartEquity` | Limiar de lucro acima do saldo que ativa o trailing de patrimônio. |
| `VolumeMultiplier` | Multiplicador aplicado ao tamanho da última ordem executada durante a piramidação. |
| `ResetEntryPrice` | Quando habilitado, limpa o preço de entrada armazenado após cada saída, evitando o escalonamento até que ocorra uma nova operação. |
| `CandleType` | Tipo de dados de vela (período) usado para cálculos de indicadores e geração de sinais. |

## Notas de implementação
- A estratégia é implementada com a API de alto nível do StockSharp. As assinaturas de velas são gerenciadas através de `SubscribeCandles`, e o indicador DeMarker é vinculado via `Bind` para que os valores do indicador cheguem como decimais prontos para uso.
- O estado do indicador é rastreado com variáveis escalares simples: o valor mais recente, o valor anterior e o valor de duas barras atrás, refletindo exatamente o padrão de acesso ao buffer do código-fonte MQL (`iDeMarkerGet(0)` e `iDeMarkerGet(2)`).
- Os volumes de ordens são arredondados de acordo com o passo de volume do instrumento e validados contra limites mínimos e máximos para evitar rejeições.
- O controle de patrimônio usa `Portfolio.CurrentValue` para espelhar as verificações de saldo/patrimônio presentes no código original. Quando o stop baseado em patrimônio dispara, a estratégia fecha todas as posições abertas através de ordens de mercado.
- O tamanho do pip é derivado de `Security.PriceStep`. Instrumentos com três ou cinco casas decimais recebem automaticamente o ajuste de dez vezes usado na versão MQL para converter pontos em pips.

## Notas de uso
- Certifique-se de que a carteira conectada forneça informações de patrimônio atual para que a lógica de trailing de patrimônio opere corretamente.
- A estratégia opera apenas em velas terminadas (`CandleStates.Finished`). Ela ignora barras parcialmente formadas, correspondendo à lógica de controle de "nova barra" do assessor especializado original.
- Os limites padrão (0.3/0.7) estão embutidos no código, mas podem ser ajustados modificando as constantes se necessário.
- A estratégia suporta trading ao vivo e backtesting. Para backtests, verifique se o simulador de carteira alimenta valores de patrimônio para permitir que a lógica de trailing de patrimônio seja executada.
