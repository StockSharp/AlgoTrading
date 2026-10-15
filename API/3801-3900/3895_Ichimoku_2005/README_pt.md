# Ichimoku Estratégia de 2005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta direta do MetaTrader consultor especialista `ichimok2005` adaptado para o StockSharp API de alto nível. Ele se concentra na identificação de rompimentos decisivos acima ou abaixo da linha Ichimoku Senkou Span B e confirma o impulso através de corpos de velas consecutivos.

## Lógica de negociação

### Configuração longa
1. Avalie as últimas `Shift + 2` velas concluídas (o padrão `Shift` é `1`, para que o algoritmo observe as três barras anteriores).
2. Exigir que:
   - A vela de referência mais antiga (`Shift + 2`) abriu abaixo do Senkou Span B.
   - A vela de referência intermediária (`Shift + 1`) abriu acima do Senkou Span B e fechou acima dele.
   - A vela de referência mais recente (`Shift`) abriu e fechou acima do Senkou Span B.
   - As duas últimas velas de referência são de alta (o preço de fechamento é maior que o preço de abertura).
3. Certifique-se de que o Ichimoku Chinkou Span não esteja preso dentro da nuvem quando o Senkou Span A estiver abaixo do Senkou Span B. Isso imita o filtro original do consultor especialista que evita fases congestionadas do mercado.
4. Se a estratégia mantiver atualmente uma posição curta, ela será fechada. Caso contrário, uma nova negociação longa será aberta, desde que o sinal anterior ainda não seja longo.

### Configuração curta
1. Espelhe as condições longas na direção oposta:
   - A vela `Shift + 2` deve abrir acima do Senkou Span B.
   - A vela `Shift + 1` deve abrir e fechar abaixo do Senkou Span B.
   - A vela `Shift` deve abrir e fechar abaixo do Senkou Span B.
   - As duas últimas velas de referência são de baixa (o preço de fechamento é menor que o preço de abertura).
2. O Chinkou Span deve ficar fora da nuvem quando o Senkou Span A estiver abaixo do Senkou Span B.
3. Feche qualquer posição longa existente e, em seguida, abra uma nova posição curta se o sinal anterior não for vendido.

As posições são gerenciadas com ordens de proteção de StockSharp. Stop Loss e Take Profit são medidos em etapas de preço e convertidos em distâncias absolutas usando o `PriceStep` do instrumento. As ordens de proteção são registradas com saídas de mercado para replicar o comportamento MetaTrader de usar paradas de mercado.

## Dimensionamento de posições

O orientador original suportava dois modos de dimensionamento:
- **Volume fixo** (`UseMoneyManagement = false`): as negociações são executadas com o parâmetro `OrderVolume` (padrão 0,1 lote).
- **Gerenciamento de dinheiro** (`UseMoneyManagement = true`): a estratégia usa o valor atual do portfólio e a porcentagem de `MaximumRisk` para derivar o tamanho do pedido. O resultado é ajustado à etapa do lote do título e nunca fica abaixo de uma única etapa.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `StopLossPoints` | Distância de stop-loss em etapas de preço. | 30 |
| `TakeProfitPoints` | Distância de lucro em etapas de preço. | 60 |
| `Shift` | Número de barras usadas como deslocamento na validação da estrutura de rompimento. | 1 |
| `OrderVolume` | Tamanho da negociação corrigido quando o gerenciamento de dinheiro está desativado. | 0,1 |
| `MaximumRisk` | Porcentagem do portfólio usada para dimensionar pedidos quando o gerenciamento de dinheiro está ativado. | 10 |
| `UseMoneyManagement` | Permite dimensionamento de posição baseado em risco. | falso |
| `TenkanPeriod` | Período Tenkan-sen do indicador Ichimoku. | 9 |
| `KijunPeriod` | Período Kijun-sen do indicador Ichimoku. | 26 |
| `SenkouBPeriod` | Período Senkou Span B do indicador Ichimoku. | 52 |
| `CandleType` | Prazo para todos os cálculos (o padrão é velas horárias). | 1 hora |

## Notas

- Apenas velas concluídas são processadas, garantindo que os valores Ichimoku sejam finais.
- A estratégia acompanha a última direção executada (`_lastSignal`) para evitar a repetição de ordens idênticas em sinais consecutivos, correspondendo ao comportamento do especialista MetaTrader.
- Se o instrumento não publicar `PriceStep`, as distâncias de stop-loss e take-profit serão tratadas como valores de preço absolutos.
