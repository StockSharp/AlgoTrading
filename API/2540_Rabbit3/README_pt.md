# Estratégia Rabbit3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista original do MetaTrader 5 `Rabbit3 (edição de barabashkakvn)`.
- Implementa a lógica na API de alto nível do StockSharp com assinaturas de velas e vinculações de indicadores.
- Concentra-se em uma dupla confirmação entre Williams %R e o Índice de Canal de Commodities (CCI) antes de empilhar posições.
- Adiciona dimensionamento dinâmico de posição: lucros acima de um limite em dinheiro aumentam o volume da ordem para o próximo sinal.

## Lógica de trading
### Condições de entrada
1. **Comprado**
   - As velas fechadas atuais e anteriores reportam Williams %R abaixo de `WilliamsOversold` (padrão `-80`).
   - O valor do CCI está abaixo de `CciBuyLevel` (padrão `-80`).
   - A posição líquida atual é não negativa e adicionar outra posição mantém a exposição dentro de `MaxPositions` × `BaseVolume` (o volume aumentado é usado quando ativo).
2. **Vendido**
   - As velas fechadas atuais e anteriores reportam Williams %R acima de `WilliamsOverbought` (padrão `-20`).
   - O valor do CCI está acima de `CciSellLevel` (padrão `80`).
   - A posição líquida atual é não positiva e a nova ordem permanece dentro do limite de empilhamento configurado.

### Saída e controle de risco
- Ordens protetoras de stop-loss e take-profit são registradas automaticamente através de `StartProtection`.
- As distâncias são expressas em "pontos ajustados": quando o instrumento usa 3 ou 5 decimais, a estratégia multiplica o passo de preço por 10 para emular a aritmética de pips do MetaTrader antes de aplicar `StopLossPips` e `TakeProfitPips`.
- Não são necessárias regras de saída manuais adicionais; as ordens protetoras fecham as operações.

### Dimensionamento de posição
- `BaseVolume` define o tamanho inicial da operação (padrão `0.01`).
- Após o fechamento de cada operação, o delta de PnL realizado é comparado com `ProfitThreshold` (padrão `4` unidades monetárias).
- Se o delta for estritamente maior, o próximo sinal usa `BaseVolume × VolumeMultiplier` (padrão `1.6`). Caso contrário, o tamanho é redefinido para `BaseVolume`.
- O volume atual também é exposto através da propriedade `Volume` da estratégia para feedback da interface.

### Indicadores e visualização
- Williams %R, CCI, uma EMA rápida (`FastEmaPeriod`) e uma EMA lenta (`SlowEmaPeriod`) estão vinculadas ao feed de velas para monitoramento e representação gráfica.
- Uma área de gráfico é criada automaticamente, exibindo velas, indicadores e operações executadas.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | período de `1h` | Tipo de dados para assinatura de velas. |
| `CciPeriod` | `15` | Comprimento do Índice de Canal de Commodities. |
| `CciBuyLevel` | `-80` | Limite do CCI que permite entradas compradas. |
| `CciSellLevel` | `80` | Limite do CCI que permite entradas vendidas. |
| `WilliamsPeriod` | `62` | Período de lookback para Williams %R. |
| `WilliamsOversold` | `-80` | Limite de sobrevenda usado para confirmação comprada. |
| `WilliamsOverbought` | `-20` | Limite de sobrecompra usado para confirmação vendida. |
| `FastEmaPeriod` | `17` | EMA rápida para contexto de tendência. |
| `SlowEmaPeriod` | `30` | EMA lenta para contexto de tendência. |
| `MaxPositions` | `2` | Número máximo de entradas empilhadas por direção. |
| `ProfitThreshold` | `4` | Lucro realizado necessário para aumentar o tamanho da próxima ordem (unidades monetárias). |
| `BaseVolume` | `0.01` | Volume base da ordem. |
| `VolumeMultiplier` | `1.6` | Multiplicador aplicado quando a condição de aumento é atendida. |
| `StopLossPips` | `45` | Distância do stop-loss em pontos ajustados. |
| `TakeProfitPips` | `110` | Distância do take-profit em pontos ajustados. |

## Notas
- A estratégia opera sobre posições líquidas. Ao contrário da versão MQL compatível com hedge, comprados e vendidos não são mantidos simultaneamente; sinais na direção oposta são ignorados até que a exposição atual seja fechada por ordens protetoras.
- `MaxPositions` funciona como um limite para a posição agregada (volume base multiplicado pelo fator de empilhamento). Ajuste-o cuidadosamente ao alterar os volumes base ou aumentados.
- A tolerância de volume usa metade do passo de volume do instrumento para absorver pequenas diferenças de arredondamento ao verificar o limite de empilhamento.
- A tradução para Python ainda não está incluída e pode ser adicionada mais tarde se necessário.
