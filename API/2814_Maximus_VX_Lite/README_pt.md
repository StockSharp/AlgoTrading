# Estratégia Maximus vX Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conversão do assessor especialista do MetaTrader 5 "maximus_vX lite" para a API de alto nível do StockSharp.
A estratégia busca zonas de consolidação acima e abaixo do preço atual e aguarda o preço se mover um número configurável
de pontos dessas zonas antes de entrar. O tamanho da posição é determinado a partir de um orçamento opcional de porcentagem
de risco, e o lucro flutuante pode acionar a liquidação forçada de toda a exposição aberta.

## Lógica da estratégia

1. **Varredura histórica** – em cada vela finalizada a estratégia mantém até `HistoryDepth` velas e usa uma janela deslizante
   `RangeLookback` para detectar máximas e mínimas compactas que formam áreas de consolidação.
2. **Canal superior** – quando um bloco superior válido é detectado, o canal é ancorado em torno do fechamento atual com uma
   largura de `RangePoints`. Se nenhum bloco histórico se qualificar, o canal volta à mesma largura ajustada ao preço atual.
3. **Canal inferior** – o bloco inferior é retirado diretamente de máximas/mínimas históricas que satisfazem as condições de
   range ou, se nenhuma existir, de um nível sintético em torno do fechamento atual menos `RangePoints`.
4. **Entradas compradas** – dois setups comprados são permitidos:
   - Rompimento acima da consolidação inferior: o preço deve exceder `_lowerMax` por `DistancePoints` e o canal superior deve
     estar disponível. O take profit usa dois terços da distância entre `_lowerMax` e `_upperMin`, com um mínimo igual a `RangePoints`.
   - Rompimento acima do canal superior: o preço deve exceder `_upperMax` por `DistancePoints`. O take profit é definido como `2 * RangePoints`.
5. **Entradas vendidas** – a lógica simétrica é acionada quando o preço cai abaixo de `_upperMin` ou `_lowerMin` por `DistancePoints`.
   O setup vendido primário também usa o objetivo dinâmico de dois terços, enquanto o secundário usa `2 * RangePoints`.
6. **Stops e saídas** – `StopLossPoints` define um stop protetor fixo quando maior que zero. `MinProfitPercent` monitora o capital
   flutuante versus o último balanço plano e fecha todas as posições assim que o limite é excedido. Verificações manuais de stop/alvo
   emulam o comportamento do assessor especialista original dentro da estratégia.
7. **Dimensionamento de posição** – quando `RiskPercent` é maior que zero e um stop é definido, o volume da ordem é calculado a partir
   do valor do portfólio e da distância do stop. Caso contrário, a estratégia reutiliza a propriedade `Volume`.

## Parâmetros

- `DelayOpen` (padrão `2`) – número de barras do período durante as quais adicionar ao mesmo lado é permitido.
- `DistancePoints` (padrão `850`) – distância mínima de uma borda de consolidação antes de entrar.
- `RangePoints` (padrão `500`) – largura das caixas de consolidação.
- `HistoryDepth` (padrão `1000`) – número de velas mantidas na memória para varreduras históricas.
- `RangeLookback` (padrão `40`) – comprimento da janela usada para calcular máximas e mínimas locais.
- `CandleType` (padrão `TimeSpan.FromMinutes(15).TimeFrame()`) – período de tempo usado para cálculos.
- `RiskPercent` (padrão `5m`) – porcentagem do valor do portfólio arriscada por trade. Definir como zero para usar volume fixo.
- `StopLossPoints` (padrão `1000`) – distância do stop protetor; zero desabilita o stop.
- `MinProfitPercent` (padrão `1m`) – porcentagem de lucro flutuante que força o fechamento de todas as posições.

## Detalhes

- **Comprado/Vendido**: Ambas as direções
- **Critérios de saída**: Stop fixo ou take profit, bloqueio de capital via `MinProfitPercent`
- **Stops**: Stop fixo opcional de `StopLossPoints`
- **Indicadores**: Nenhum (price action puro com análise de janela deslizante)
- **Período**: Configurável via `CandleType` (padrão 15 minutos)
- **Complexidade**: Intermediário (combina varredura de histórico, alvos dinâmicos e dimensionamento de risco)
- **Nível de risco**: Alto quando o percentual de risco é usado devido à natureza de rompimento
