# Estratégia EA Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Port de alto nível para StockSharp do consultor especialista MetaTrader "EA Stochastic". A estratégia se inscreve em uma série de velas, lê
os valores do oscilador estocástico e mantém no máximo uma posição líquida. As entradas ocorrem quando a linha principal do
estocástico permaneceu no mesmo lado dos limiares configurados por um número configurável de barras. Saídas de proteção e um trailing
stop espelham a implementação MQL original usando distâncias baseadas em pips.

## Visão geral da estratégia

- **Indicador**: oscilador estocástico clássico (componentes `%K` e `%D` com suavização configurável)
- **Direção**: comprado e vendido
- **Posicionamento**: uma única posição de cada vez (novas operações são ignoradas enquanto uma ordem de saída está pendente)
- **Tipo de ordem**: ordens a mercado usando volume fixo
- **Dados**: um único tipo de vela selecionado pelo usuário (padrão: velas de 15 minutos)

## Lógica de entrada

1. O valor principal do estocástico é armazenado em cada vela concluída.
2. Após pelo menos `ComparedBar` valores em cache, comparar o `kValue` atual com o valor de `ComparedBar - 1` velas atrás.
3. **Ir Comprado** quando ambos os valores estiverem abaixo de `UpperLevel`. Isso corresponde ao EA original que só compra quando o oscilador permaneceu
   abaixo do limiar superior pelo comprimento de lookback configurado.
4. **Ir Vendido** quando ambos os valores estiverem acima de `LowerLevel`. O EA original permitia vendidos sempre que o estocástico permanecesse acima do limite
   inferior.
5. Novas entradas são ignoradas se uma posição existir ou se uma saída de proteção já tiver sido solicitada para a posição atual.

## Saída e gestão de risco

- **Stop Loss**: distância opcional fixa de pips a partir do preço de entrada. Stops são avaliados contra mínimas de vela (para comprados) ou máximas
  (para vendidos).
- **Take Profit**: alvo fixo opcional em pips. Verificações de máxima/mínima emulam o comportamento de take profit baseado em ordens do MetaTrader.
- **Trailing Stop**: ativado quando a operação aberta ganha mais de `(TrailingStopPips + TrailingStepPips)` pips. O stop é então
  movido para `TrailingStopPips` atrás do último extremo, respeitando o gap do passo de trailing como o EA original.
- **Ordens de saída**: os fechamentos são emitidos com ordens a mercado (`SellMarket` / `BuyMarket`). Um indicador de guarda impede ordens de saída repetidas
  até que `OnPositionChanged` confirme o estado plano.

## Parâmetros

- `StopLossPips` (padrão **50**): distância em pips usada para o stop de proteção inicial. Definir como zero para desabilitar.
- `TakeProfitPips` (padrão **150**): distância em pips para realização de lucro. Definir como zero para desabilitar.
- `TrailingStopPips` (padrão **15**): distância de trailing em pips. Deve ser maior que zero se o trailing estiver habilitado.
- `TrailingStepPips` (padrão **5**): progresso mínimo em pips necessário antes do trailing stop ser atualizado. O trailing é rejeitado quando
  este valor é zero.
- `Volume` (padrão **1**): volume de ordem a mercado usado para operações compradas e vendidas.
- `KPeriod` (padrão **5**): comprimento de lookback para a linha estocástica %K.
- `DPeriod` (padrão **3**): comprimento de suavização para a linha %D.
- `Slowing` (padrão **3**): suavização final aplicada ao cálculo de %K.
- `UpperLevel` (padrão **80**): limiar usado para validar configurações compradas.
- `LowerLevel` (padrão **20**): limiar usado para validar configurações vendidas.
- `ComparedBar` (padrão **3**): número de barras a olhar atrás ao validar os níveis estocásticos (mínimo 1).
- `CandleType` (padrão **velas de 15 minutos**): série de velas inscrita pela estratégia.

## Notas de implementação

- O tamanho do pip é aproximado a partir de `Security.PriceStep`. Para instrumentos com pips fracionários (pares FX típicos) passos menores que
  `0.001` são automaticamente multiplicados por 10, reproduzindo a lógica `digits_adjust` do MetaTrader.
- A configuração do trailing stop é validada no início para evitar o caso de erro do EA original (`TrailingStop > 0` com passo de trailing zero).
- O oscilador estocástico do StockSharp usa suavização padrão e modos de preço (fechamento/máxima/mínima), que corresponde às configurações do EA de
  média móvel simples sobre intervalos máxima/mínima.
- O EA original fornecia tanto lote fixo quanto dimensionamento de posição por percentual de risco. Este port mantém o parâmetro fixo `Volume` e pode
  ser estendido se o dimensionamento baseado em percentual for necessário.
- A saída do gráfico renderiza as velas processadas, o indicador estocástico e as operações executadas para facilitar a depuração.

## Uso sugerido

- Funciona em períodos intradiários ou superiores; ajuste `CandleType` e os períodos estocásticos para se adequar ao instrumento.
- Ajuste `UpperLevel`, `LowerLevel` e `ComparedBar` para diferentes regimes de mercado (range vs. tendência).
- Combine com controles de risco do lado do broker em operações ao vivo porque as saídas são simuladas por ordens a mercado após a
  confirmação de vela.
