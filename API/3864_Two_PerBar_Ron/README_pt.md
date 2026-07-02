# Estratégia TwoPerBar Ron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O especialista original MetaTrader "TwoPerBar" de Ron Thompson abre **duas ordens de mercado no início de cada nova barra** – uma longa e uma curta. Sempre que uma perna atinge uma meta de caixa fixa (`ProfitMade * Point` no código MQL), ela é fechada e, na abertura da próxima barra, qualquer exposição restante é liquidada antes que um novo par coberto seja criado. Se a barra anterior terminou com posições abertas, o tamanho do lote é duplicado até um limite de segurança (`LotLimit`). A porta StockSharp reproduz esse comportamento usando a estratégia de alto nível API, cotações de nível 1 para monitoramento de oferta/venda e rastreamento explícito das duas pernas protegidas.

## Fluxo de trabalho de negociação
1. **Detecção de barra** – `SubscribeCandles(CandleType)` notifica a estratégia quando a série de velas configurada termina. Uma vela concluída marca o início de uma nova barra, assim como a mudança `Time[0]` de MetaTrader.
2. **Inspeção de lucro** – Instantâneos de nível 1 (oferta/venda) são monitorados continuamente. Assim que o melhor lance ou venda se afastar o suficiente do preço de entrada registrado, a perna correspondente é fechada com `SellMarket` ou `BuyMarket`.
3. **Liquidação forçada** – no início de uma nova barra, quaisquer pernas sobreviventes são fechadas no mercado. Isso reflete o loop `OrderClose` no script MQL.
4. **Escalonamento de volume** – quando o ciclo anterior tinha negociações ativas, o tamanho do lote é multiplicado por `VolumeMultiplier` (padrão `2`). Caso contrário, ele será redefinido para `BaseVolume`. O valor é normalizado em relação à etapa de volume do instrumento e fixado por `MaxVolume` e pela troca `Security.MaxVolume`.
5. **Criação de hedge** – duas ordens de mercado são enviadas via `BuyMarket` e `SellMarket`. Cada perna lembra seu volume alvo, o tamanho real preenchido e o preço médio ponderado de preenchimento para que as verificações de lucro operem com informações precisas.

## Gestão de risco e dinheiro
- **Martingale escalonamento de estilo** – dobrar o lote após um ciclo inacabado imita o dimensionamento original semelhante ao martingale. Quando ambas as pernas fecham durante a barra, a sequência é redefinida para o lote base.
- **Metas de lucro por trecho** – `ProfitTargetPoints` traduz a entrada MetaTrader `ProfitMade`. O valor é multiplicado pelo tamanho do ponto do instrumento e comparado com o bid/ask para decidir quando sair de uma perna.
- **Conformidade cambial** – `NormalizeVolume` garante que os lotes gerados respeitem o instrumento `VolumeStep` e `MinVolume`. Valores superdimensionados acionam uma redefinição para uma quantidade negociável.
- **Contabilidade coberta** – a estratégia mantém sua própria lista de pernas, porque StockSharp carteiras normalmente expõem apenas posições líquidas. Isto permite que os ambientes que suportam contas cobertas sigam o mesmo comportamento.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Velas de 1 minuto | Período primário que sinaliza quando uma nova barra foi iniciada. |
| `BaseVolume` | `decimal` | `0.1` | Tamanho inicial do lote para um ciclo totalmente novo. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplicador aplicado após uma barra terminar com posições abertas. |
| `MaxVolume` | `decimal` | `12.8` | Teto rígido para o tamanho do lote martingale. |
| `ProfitTargetPoints` | `decimal` | `19` | Meta de lucro expressa em pontos; multiplicado pelo tamanho do ponto do instrumento e comparado às cotações de compra/venda. |

## Diferenças da versão MQL
- Usa `SubscribeLevel1()` em vez de globais `Bid`/`Ask` tick-by-tick, mas mantém a mesma lógica com base nas melhores cotações.
- Os pedidos são enviados por meio de métodos auxiliares StockSharp (`BuyMarket`, `SellMarket`) para que todos os arredondamentos específicos da bolsa ocorram automaticamente.
- O tratamento de volume respeita `VolumeStep`, `MinVolume` e `MaxVolume`, enquanto o script original funcionava com valores duplos brutos.
- A porta StockSharp armazena informações de trecho internamente; os conectores executados no modo de compensação ainda podem nivelar os hedges, portanto, confirme se o seu corretor oferece suporte a posições opostas.

## Dicas de uso
- Combine `BaseVolume` com um tamanho de lote válido para o instrumento selecionado; caso contrário, a etapa de normalização irá ignorar a negociação.
- Mantenha `ProfitTargetPoints` alinhado com o tamanho do ponto do símbolo – valores excessivamente grandes raramente serão atingidos dentro de uma única barra.
- Como a estratégia envia ordens de mercado opostas, execute-a em fontes de dados de demonstração ou contas de hedge antes de passar para ambientes de produção.
- Anexe a estratégia a um gráfico: `OnStarted` adiciona velas e negociações executadas ao gráfico visual para facilitar o monitoramento.
