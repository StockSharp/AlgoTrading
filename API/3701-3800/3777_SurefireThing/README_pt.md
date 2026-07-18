# Estratégia SurefireThing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia SurefireThing é uma versão StockSharp de alto nível do consultor especialista MetaTrader 4 *Surefirething*. Ele opera em velas concluídas, calcula os níveis de ordens pendentes do intervalo da sessão anterior e redefine a exposição na virada de cada dia de negociação. A lógica está centrada na implantação de um par simétrico de ordens limitadas que tentam capturar a reversão à média em torno do fechamento anterior.

## Lógica de negociação
- No final de cada dia de negociação, a estratégia tenta nivelar a posição e cancela quaisquer ordens pendentes ativas.
- Usando a última vela concluída do dia anterior, ele mede a faixa de preço `(High - Low)` e multiplica-a por `RangeMultiplier` (o padrão é 1,1 como no EA original).
- Metade do intervalo ajustado é adicionado ao fechamento anterior para obter o preço de entrada com limite de venda. A mesma distância é subtraída do fechamento para colocar a ordem com limite de compra.
- As compensações de stop-loss e take-profit são expressas em etapas de preços. Quando o instrumento expõe um `Security.Step` válido, eles são convertidos em distâncias absolutas e gerenciados por meio de `StartProtection` para que as posições preenchidas recebam ordens de proteção automaticamente.
- As ordens são enviadas uma vez por dia de negociação. Se ocorrerem preenchimentos, a proteção anexada trata das saídas; caso contrário, os pedidos permanecerão ativos até a próxima reinicialização diária.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume enviado com cada ordem pendente. | `0.1` |
| `TakeProfitPoints` | Distância (em etapas de preço) para a meta de lucro. Convertido em um deslocamento absoluto quando a etapa é conhecida. | `10` |
| `StopLossPoints` | Distância (em etapas de preço) para o stop de proteção. Convertido da mesma forma que a meta de lucro. | `15` |
| `RangeMultiplier` | Fator aplicado ao intervalo de velas anterior antes de calcular os preços de entrada. | `1.1` |
| `CandleType` | Período primário processado pela estratégia. O padrão é velas de 1 minuto, mas pode ser ajustado para corresponder ao gráfico original. | `TimeSpan.FromMinutes(1)` |

## Notas de implementação
- API de alto nível: as velas são consumidas por meio de `SubscribeCandles(CandleType)` e processadas no manipulador `ProcessCandle` assim que são concluídas.
- Redefinição diária: `CloseForNewDay` cancela ordens pendentes e fecha posições sempre que um novo dia de calendário é detectado a partir de carimbos de data e hora de velas.
- Lógica de proteção: `ConfigureProtection` traduz os controles de risco baseados em pontos em `Unit` instâncias e ativa `StartProtection` para que as ordens stop-loss e take-profit sejam recriadas automaticamente após o preenchimento.
- Ciclo de vida do pedido: as referências a ambos os pedidos pendentes são armazenadas e limpas via `CancelPendingOrder`, bem como `OnOrderChanged` quando os pedidos são concluídos ou cancelados.
- Normalização de preços: `Security.ShrinkPrice` é usado para arredondar os preços calculados para o tamanho do tick do instrumento antes de enviar novos pedidos.

## Recomendações de uso
- Alinhe `CandleType` com o período de tempo usado pelo EA original (normalmente o gráfico onde foi anexado) para manter as mesmas velas de referência.
- Ajuste `RangeMultiplier` quando os instrumentos apresentam diferentes características de volatilidade para que as ordens pendentes permaneçam dentro de distâncias realistas.
- Se o corretor impor distâncias mínimas de parada, certifique-se de que `TakeProfitPoints` e `StopLossPoints` respeitem essas restrições após a conversão para preços absolutos.
- A estratégia pressupõe dados intradiários contínuos. Quando ocorrem grandes lacunas (fins de semana, feriados), a próxima vela disponível ainda aciona um reset e uma nova colocação de pedido com base na última barra observada.
