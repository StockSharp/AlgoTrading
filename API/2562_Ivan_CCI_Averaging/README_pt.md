# Estratégia de Médio Custo CCI de Ivan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Port do consultor especialista MetaTrader "Ivan" que opera em extremos de CCI com entradas de médio custo e stop de média móvel suavizada. A estratégia monitora um CCI(100) de longo prazo para estabelecer regimes globais de compra ou venda, opcionalmente adiciona posições quando o CCI(13) retrocede, e gerencia o risco com lógica de break-even e trailing em torno de uma média móvel suavizada. O dimensionamento de posição espelha o modelo de risco percentual original e um coeficiente de proteção de lucros fecha o livro quando o equity se multiplica.

## Detalhes

- **Critérios de entrada**:
  - **Sinal global comprado**: O CCI(100) sobe acima de `GlobalSignalLevel` enquanto nenhum regime de compra está ativo. Uma ordem comprada de mercado é enviada com o stop inicial no valor da MA suavizada, desde que o stop esteja pelo menos `MinStopDistance` abaixo do preço.
  - **Médio custo comprado**: Se `UseAveraging` estiver habilitado e a bandeira global de compra estiver definida, qualquer queda do CCI(13) abaixo de `-GlobalSignalLevel` adiciona outro comprado usando o mesmo modelo de stop.
  - **Sinal global vendido**: O CCI(100) cai abaixo de `-GlobalSignalLevel` enquanto nenhum regime de venda está ativo, ativando uma entrada vendida quando o stop da MA está pelo menos `MinStopDistance` acima do preço.
  - **Médio custo vendido**: Com `UseAveraging` habilitado, uma alta do CCI(13) acima de `GlobalSignalLevel` dentro de um regime de venda adiciona à exposição vendida.
- **Comprado/Vendido**: Opera em ambas as direções e pode piramidizar posições dentro do viés ativo.
- **Critérios de saída**:
  - Voltar a cruzar dentro de `±ReverseLevel` no CCI(100) cancela ambos os regimes e força a exposição a zero.
  - O equity do portfólio que excede `ProfitProtectionFactor` vezes o saldo inicial força a liquidação de todas as posições.
  - Atingir o preço de stop rastreado (break-even ou trailing de MA) fecha a parte da posição.
- **Stops**:
  - O stop inicial vem de uma média móvel suavizada (SMMA) do período `StopLossMaPeriod`.
  - O break-even move o stop para o preço de entrada uma vez que o preço avança `BreakEvenDistance` (definir como zero para desabilitar).
  - O trailing ajusta o stop apenas se a MA progredir pelo menos `TrailingStep` além do stop atual.
- **Filtros**:
  - `UseZeroBar` replica a opção MT5 de ler a barra recém-aberta ou a última barra fechada para comparações de sinais.
  - `MinStopDistance` previne operações quando o stop da MA está muito próximo do preço.
- **Dimensionamento de posição**:
  - Cada nova ordem arrisca `RiskPercent` do valor atual do portfólio dividido pela distância entre o preço e o stop, com `MinimumVolume` como piso de segurança.

## Parâmetros

- **Use Averaging** *(bool, padrão: true)* — Habilitar ordens de médio custo adicionais durante um regime global ativo.
- **Stop MA Period** *(int, padrão: 36)* — Período da MA suavizada usada para derivar níveis de stop.
- **Risk %** *(decimal, padrão: 10)* — Porcentagem do equity da conta a arriscar em cada nova operação.
- **Use Zero Bar** *(bool, padrão: true)* — Se verdadeiro, usa os valores da vela mais recente; caso contrário os sinais se baseiam na barra fechada anterior.
- **Reverse Level** *(decimal, padrão: 100)* — Limiar absoluto de CCI que cancela ambos os regimes e fecha todas as posições.
- **Global Level** *(decimal, padrão: 100)* — Limiar absoluto de CCI que ativa um novo sinal global de compra ou venda.
- **Min Stop Distance** *(decimal, padrão: 0.005)* — Separação mínima de preço entre a entrada e o stop da MA (0.005 ≈ 50 pips em pares FX de 5 dígitos).
- **Trailing Step** *(decimal, padrão: 0.001)* — Melhoria mínima necessária antes que o stop trailing de MA avance.
- **BreakEven Distance** *(decimal, padrão: 0.0005)* — Movimento de preço necessário para deslocar o stop para o preço de entrada; definir como 0 para desabilitar o break-even.
- **Profit Protection** *(decimal, padrão: 1.5)* — Múltiplo de equity que aciona a liquidação total para garantir lucros.
- **Minimum Volume** *(decimal, padrão: 1)* — Tamanho de operação de reserva quando o dimensionamento baseado em risco resulta em volume pequeno ou zero.
- **Candle Type** *(DataType)* — Série de velas usada para indicadores (período padrão de 15 minutos).

## Notas

- Distâncias como `MinStopDistance`, `TrailingStep` e `BreakEvenDistance` são expressas em unidades de preço e devem ser ajustadas ao tamanho do tick do instrumento.
- A estratégia pressupõe execuções síncronas das ordens `BuyMarket`/`SellMarket`; ajustar as configurações de execução se slippage ou execuções parciais forem esperados.
- O dimensionamento baseado em portfólio requer um adaptador de portfólio conectado; caso contrário, `MinimumVolume` é usado para todas as ordens.
