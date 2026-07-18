# Estratégia de Controle de Portfólio de Futuros com Expiração
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reconstrói o assessor especialista do MetaTrader 5 *Futures Portfolio Control Expiration* sobre a API de alto nível do StockSharp. Mantém um portfólio de futuros de três pernas, preserva a exposição comprada/vendida desejada para cada perna e rola automaticamente cada contrato para o próximo vencimento quando a vida útil restante cai abaixo de um limite configurável.

A implementação replica o fluxo de trabalho original:
1. Identificar o contrato atualmente negociável para cada família de futuros com base em um código curto (por exemplo `MXI` ou `BR`).
2. Abrir ou ajustar a posição para que o volume real do portfólio corresponda ao valor de lote configurado (positivo = comprado, negativo = vendido).
3. Monitorar o tempo de vencimento em cada vela finalizada de uma assinatura de heartbeat.
4. Fechar o contrato que expira, descobrir o próximo vencimento na mesma família e recriar a exposição alvo no novo contrato.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `BoardCode` | Quadro de bolsa anexado aos identificadores de futuros (por exemplo `FORTS`). Deixe vazio se o provedor não exigir um sufixo de quadro. | `FORTS` |
| `Symbol1`, `Symbol2`, `Symbol3` | Códigos curtos das três famílias de futuros. A estratégia itera os vencimentos de futuros construindo identificadores como `CODE-M.YY`. | `MXI`, `BR`, `SBRF` |
| `Lot1`, `Lot2`, `Lot3` | Tamanho de posição alvo por perna. Valores positivos criam exposição comprada, valores negativos criam exposição vendida. | `-4`, `-1`, `5` |
| `HoursBeforeExpiration` | Número de horas antes do vencimento do contrato quando o roll deve começar. | `25` |
| `MonitoringCandleType` | Tipo de vela usado apenas como heartbeat para acionar verificações de vencimento (por exemplo velas horárias). | Período `1H` |

## Gestão de roll e posição
- **Descoberta de contratos.** Para cada perna a estratégia escaneia até doze meses consecutivos do calendário. Tenta múltiplos formatos de identificador (`CODE-M.YY`, `CODE-MM.YY`, `CODEMMYY`, `CODEMYY`) e opcionalmente anexa o `BoardCode` configurado. Apenas instrumentos com data de vencimento posterior ao tempo de referência são elegíveis.
- **Atualizações de heartbeat.** Uma assinatura de velas em cada contrato ativo fornece um callback de vela finalizada que reavalia os temporizadores de vencimento e sincroniza a exposição do portfólio.
- **Lógica de roll.** Quando a vida útil restante é menor ou igual a `HoursBeforeExpiration`, a estratégia fecha qualquer posição aberta no contrato atual, localiza o próximo futuro com vencimento posterior, re-assina as velas de heartbeat e restaura o lote alvo no novo contrato.
- **Sincronização de posição.** Após cada heartbeat, a posição real é comparada ao lote alvo. A estratégia aumenta ou diminui a exposição com ordens a mercado para que a posição ao vivo sempre corresponda ao volume solicitado (incluindo zero).

## Notas de uso
1. Certifique-se de que o `SecurityProvider` conheça todos os símbolos de futuros para as famílias selecionadas. Configure `BoardCode` se sua fonte de dados exigir identificadores como `Si-9.23@FORTS`.
2. Inicie a estratégia com os parâmetros de portfólio desejados. As posições são abertas apenas quando a estratégia está online e o trading é permitido.
3. A estratégia registra cada atribuição, ajuste e evento de roll. Use essas mensagens para verificar o mapeamento entre códigos curtos e futuros reais.
4. Como a assinatura de heartbeat é apenas um temporizador, você pode escolher qualquer tipo de vela que esteja disponível de forma consistente para os instrumentos negociados.

## Detalhes de implementação
- Os componentes da API de alto nível (`SubscribeCandles`, `StrategyParam`, `BuyMarket`/`SellMarket`) mantêm o código conciso e aderem às diretrizes do projeto.
- Nenhuma coleção personalizada de dados históricos é armazenada; a estratégia trabalha apenas com o último evento de vela e o estado da posição.
- Comentários em inglês dentro do código descrevem cada etapa importante para facilitar a manutenção.
