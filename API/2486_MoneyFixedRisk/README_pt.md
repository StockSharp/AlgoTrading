# Estratégia de Risco Fixo de Capital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Risco Fixo de Capital é um port direto do consultor especialista do MetaTrader 5 **Money Fixed Risk.mq5**. O script original calcula periodicamente o tamanho máximo de posição que mantém o risco abaixo de uma porcentagem fixa do capital da conta e então abre uma compra de mercado protegida com ordens simétricas de stop-loss e take-profit. Esta versão do StockSharp preserva o mesmo comportamento utilizando a API de assinatura de ticks de alto nível e os controles de risco fornecidos pelo framework.

A estratégia escuta cada negociação (tick) do instrumento selecionado. Após um número configurável de ticks, avalia o valor atual do portfólio, converte a distância de stop configurada em pips para unidades de preço e calcula o maior volume que mantém o risco dentro da porcentagem do capital especificada. Se o volume calculado for válido, a estratégia abre uma ordem de compra de mercado e atribui níveis de stop-loss e take-profit exatamente à distância do stop a partir do preço executado. O stop e o alvo são monitorados em cada tick subsequente e a posição é fechada quando qualquer um dos limites é tocado.

## Requisitos de dados
- Dados de ticks (negociações) são necessários porque a condição de entrada conta ticks individuais. Dados de velas não são utilizados.
- `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` e o opcional `MaxVolume` devem estar corretamente configurados para o instrumento de modo que a fórmula de dimensionamento de posição corresponda às especificações do contrato do broker.

## Como a estratégia funciona
1. Aguardar atualizações de tick via `SubscribeTrades()`.
2. Rastrear o último preço negociado e incrementar um contador interno.
3. Sempre que o contador de ticks atingir o **Ticks Interval**, zerar o contador e:
   - Determinar o tamanho do pip a partir de `PriceStep` e `Decimals` (cotações de 5 e 3 dígitos são automaticamente escaladas por 10).
   - Converter a distância de stop-loss configurada de pips para unidades de preço.
   - Determinar o capital atual da conta (tenta `Portfolio.CurrentValue`, recorre a `CurrentBalance`, depois `BeginValue`).
   - Calcular o risco monetário por contrato usando a distância do stop e `StepPrice`.
   - Derivar o volume máximo que mantém o risco monetário abaixo de `Risk %` do capital e normalizá-lo para o passo de volume e limites da bolsa.
4. Se o volume calculado for positivo, enviar uma ordem de compra de mercado dimensionada para achatar qualquer exposição vendida existente e abrir uma nova posição comprada.
5. Registrar os preços de stop-loss e take-profit em torno do preço de entrada. Em cada tick subsequente monitorar o preço de negociação e fechar a posição se qualquer nível for violado.

## Parâmetros
- **Stop Loss (pips)** – distância do stop-loss expressa em pips. O take-profit é colocado à mesma distância na direção oposta.
- **Risk %** – porcentagem do capital do portfólio arriscada em cada operação.
- **Ticks Interval** – número de ticks a aguardar antes de reavaliar e potencialmente abrir uma nova posição.

Todos os parâmetros suportam otimização e validação (devem ser maiores que zero).

## Detalhes de gestão de capital
- Valor em risco = `Equity * (Risk % / 100)`.
- Distância do stop em unidades de preço = `Stop Loss (pips) * pip size`, onde pip size equivale a `PriceStep * 10` para instrumentos de 3 e 5 decimais; caso contrário `PriceStep`.
- Risco monetário por contrato = `(stop distance / PriceStep) * StepPrice`.
- Tamanho da posição = `Risk amount / monetary risk per contract`, arredondado para baixo para o `VolumeStep` mais próximo e restrito por `MinVolume`/`MaxVolume`. Ordens são ignoradas quando o tamanho normalizado está abaixo do volume mínimo.

## Diferenças em relação ao consultor especialista original
- Executa completamente dentro do StockSharp sem chamar bibliotecas do MetaTrader.
- Usa `StartProtection()` para que as proteções em nível de plataforma permaneçam ativas.
- Depende do portfólio da estratégia para informações do capital atual em vez de consultar objetos de saldo do terminal.
- Usa monitoramento contínuo de ticks para sair de posições, eliminando a necessidade de ordens de stop explícitas neste exemplo educativo.

## Notas de uso
- Este exemplo abre apenas posições compradas assim como o arquivo original. Estender `ProcessTrade` se operações vendidas forem necessárias.
- Ao fazer backtesting, certifique-se de que os dados de ticks incluam profundidade suficiente para atingir o intervalo de ticks configurado; caso contrário nenhuma operação será disparada.
- Como o dimensionamento de posição depende dos metadados do broker, verificar a correção de `PriceStep`, `StepPrice` e restrições de volume antes de operar ao vivo.
- A implementação evita usar coleções de indicadores para respeitar as diretrizes de conversão e mantém a lógica com estado através de campos privados.
