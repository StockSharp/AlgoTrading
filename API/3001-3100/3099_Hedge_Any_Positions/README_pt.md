# Estratégia de Cobertura de Qualquer Posição
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Cobertura de Qualquer Posição** é uma conversão direta do expert MQL5 original *Hedge any positions (barabashkakvn's edition)*. A versão do StockSharp mantém a ideia central intacta: ela monitora cada perna aberta criada pela estratégia e, uma vez que uma perna perde um número definido de pips, imediatamente abre uma posição oposta com um tamanho de lote amplificado. A implementação se baseia na API de alto nível do StockSharp, portanto as ordens de hedge são colocadas por meio de ordens a mercado e o rastreamento de posições é gerenciado internamente sem código de roteamento de ordens personalizado.

A estratégia pode opcionalmente colocar um trade inicial quando começa. Depois ela simplesmente reage a movimentos adversos de preço e constrói uma escada de trades de cobertura, marcando cada perna como coberta para que a mesma posição não possa desencadear múltiplas entradas opostas.

## Fluxo de trabalho de cobertura
1. **Feed de velas** – um `CandleType` configurável impulsiona a estratégia. Apenas velas terminadas são processadas.
2. **Cálculo de perdas** – a cada fechamento de vela a estratégia verifica se o preço de fechamento se moveu contra qualquer perna aberta pelo menos `LosingPips` multiplicado pelo tamanho de pip calculado.
3. **Execução do hedge** – se uma perna perdedora for encontrada, uma ordem a mercado na direção oposta é enviada. O volume da ordem equivale ao volume da perna original multiplicado por `LotCoefficient`, arredondado para o passo de volume do instrumento e limitado ao volume mínimo/máximo permitido.
4. **Atualização de estado** – uma vez que uma ordem oposta é despachada, a perna original é marcada como coberta e o trade recém-aberto é armazenado como uma nova perna que pode ser coberta mais tarde se o preço reverter novamente.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Período usado para avaliar os movimentos de preço e acionar coberturas. | Velas de 1 minuto |
| `LosingPips` | Número de pips que o preço deve se mover contra uma perna antes de abrir uma cobertura. | 5 |
| `LotCoefficient` | Multiplicador aplicado ao volume original ao enviar a ordem de cobertura. | 2.0 |
| `AutoPlaceInitialTrade` | Quando habilitado a estratégia envia o primeiro trade automaticamente ao iniciar. | Desabilitado |
| `InitialVolume` | Tamanho da ordem usado pelo trade inicial opcional. Arredondado para o passo de volume do instrumento. | 0.10 |
| `InitialDirection` | Lado (compra ou venda) usado para o trade inicial opcional. | Compra |

> **Nota:** Definir a propriedade `Strategy.Volume` para o tamanho base de ordem que se deseja que a estratégia use. Os parâmetros acima controlam apenas o comportamento específico de cobertura.

## Diretrizes de uso
1. Atribuir um `Security`, `Portfolio` e `Volume` base desejado antes de iniciar a estratégia.
2. Ajustar `LosingPips` e `LotCoefficient` para refletir a volatilidade e a tolerância ao risco do instrumento selecionado.
3. Habilitar `AutoPlaceInitialTrade` se quiser que a versão do StockSharp crie a primeira posição automaticamente; caso contrário, abrir manualmente uma perna inicial ou deixar outro componente fazê-lo.
4. Como a API de alto nível do StockSharp trabalha com posições líquidas, a lista de pernas interna é usada para emular a estrutura de hedge. Monitorar a exposição da conta ao executar em contas de netting.
5. Revisar os relatórios de execução: cada cobertura é colocada com uma ordem a mercado (`BuyMarket` ou `SellMarket`).

## Diferenças do expert original
- A validação de margem, as verificações de slippage e o registro detalhado de resultados foram removidos; o StockSharp já reporta os problemas de execução por meio dos eventos de estratégia.
- A conversão usa velas terminadas em vez de dados tick a tick. Escolher um período suficientemente pequeno se forem necessários tempos de reação mais rápidos.
- O arredondamento de lotes agora se baseia em `Security.VolumeStep`, `Security.MinVolume` e `Security.MaxVolume` para manter a conformidade com as regras de trading do instrumento.
- Alertas, notificações e o trade inicial aleatório apenas para o testador da versão MQL foram intencionalmente omitidos. O parâmetro de entrada automática opcional substitui esse comportamento.

## Aprimoramentos recomendados
- Combinar o módulo de cobertura com uma estratégia de entrada separada que define quando a primeira posição deve ser criada.
- Adicionar regras de encerramento baseadas em capital ou limites de profundidade máxima para evitar cadeias de cobertura ilimitadas.
- Integrar monitoramento no nível do portfólio para garantir que os requisitos de margem permaneçam dentro de limites aceitáveis.
