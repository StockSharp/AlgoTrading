# Estratégia Trend Me Leave Me
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **Trend Me Leave Me** é um port direto do clássico consultor especialista MQL5 de Yury Reshetov. Ela espera
pacientemente por períodos de ação de preço tranquila, segue a direção predominante indicada pelo Parabolic SAR e alterna
a direção da operação após saídas rentáveis. Quando uma operação é stopada, a estratégia tentará a mesma direção novamente,
recriando o comportamento original "trend me, leave me". Esta implementação em C# usa a API de alto nível do StockSharp
e mantém o fluxo de decisão completo do sistema fonte enquanto expõe cada entrada numérica como um parâmetro configurável.

## Ideias principais
### Filtro de mercado calmo
- O Average Directional Index (ADX) com comprimento `AdxPeriod` mede a força direcional.
- Apenas quando a média móvel do ADX cai abaixo de `AdxQuietLevel` a estratégia permite novas entradas, imitando o foco
  do EA em retrocessos de baixa volatilidade.

### Alinhamento SAR para temporização
- Os pontos do Parabolic SAR atuam como guia direcional. Um sinal comprado requer que o fechamento da vela esteja acima
  do ponto SAR, enquanto um sinal vendido requer um fechamento abaixo do ponto.
- Os parâmetros `SarStep` e `SarMax` correspondem às configurações de aceleração da versão MQL e podem ser otimizados se necessário.

### Agendador de direção
- Um flag `TradeDirections` representa a variável original `cmd`. Começa no estado *compra*.
- Após uma saída por **take-profit** o flag muda para o lado oposto, convidando uma operação de reversão.
- Após uma saída por **stop-loss** (ou breakeven) o flag permanece no mesmo lado para que a próxima oportunidade retente
  a direção anterior.

## Gestão de operações
- `StopLossPips` e `TakeProfitPips` definem distâncias fixas a partir do preço médio de execução. Definir qualquer
  parâmetro como `0` desabilita a proteção correspondente.
- `BreakevenPips` move o stop para o preço de entrada assim que o mercado se desloca a favor pela distância de pips
  especificada. Se o preço posteriormente retornar ao nível de entrada, a operação é fechada com lucro aproximadamente
  nulo, o que mantém o próximo sinal no mesmo lado.
- A lógica de stop/take é avaliada em cada vela completa usando tanto a máxima quanto a mínima para aproximar os
  toques intrabarra, preservando o comportamento tick a tick do EA o mais fielmente possível em um ambiente baseado em barras.

## Dimensionamento da posição
- O volume da ordem é controlado pela propriedade base `Strategy.Volume`. O exemplo mantém o modelo de risco simples e
  não inclui o objeto de gerenciamento de dinheiro de risco fixo do script MQL. Ajuste `Volume` ou sobrescreva a
  estratégia se um dimensionamento mais avançado for necessário.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `StopLossPips` | Distância em pips entre o preço de entrada e o stop de proteção. | `50` |
| `TakeProfitPips` | Distância em pips entre o preço de entrada e o alvo. | `180` |
| `BreakevenPips` | Mover o stop para a entrada após este número de pips de movimento favorável. | `5` |
| `AdxPeriod` | Período de suavização para o filtro ADX. | `14` |
| `AdxQuietLevel` | Leitura máxima de ADX que ainda qualifica como mercado calmo. | `20` |
| `SarStep` | Passo de aceleração do Parabolic SAR. | `0.02` |
| `SarMax` | Fator de aceleração máximo do Parabolic SAR. | `0.2` |
| `CandleType` | Período usado para cálculos. | Velas de `1h` |

## Notas de implementação
- Os cálculos de pips seguem o ajuste de dígitos do EA: se o instrumento usa 3 ou 5 casas decimais, o passo de preço
  é multiplicado por 10 para converter o tamanho do tick do broker em um pip padrão.
- As ligações de indicadores dependem da API de alto nível do StockSharp, e todas as ações de trading usam
  `BuyMarket`/`SellMarket` para manter conformidade com as convenções do S#.
- Ainda não há tradução para Python. O diretório `PY/` está intencionalmente ausente conforme solicitado.
- Anexe a estratégia a qualquer símbolo suportado pelo StockSharp. Defina `Volume` antes de iniciar a estratégia e
  ajuste os parâmetros para corresponder à volatilidade do instrumento.
