# Estratégia de troca de símbolos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de troca de símbolos** é a porta StockSharp do utilitário MetaTrader 5 "Symbol Swap". O programa MQL5 original abre um painel onde um trader pode inserir qualquer ticker, mudar imediatamente o gráfico atual para esse símbolo e monitorar uma janela de dados compacta com o horário mais recente, preços OHLC, volume de ticks e spread. Esta conversão C# mantém as mesmas responsabilidades enquanto depende exclusivamente da assinatura de alto nível API de StockSharp.

## Comportamento

1. No início, a estratégia resolve o instrumento a ser observado. Primeiro ele tenta `WatchedSecurityId`; se o campo estiver vazio, ele volta para `Strategy.Security` que está configurado no inicializador.
2. Os dados da vela do `CandleType` escolhido são transmitidos por meio de `SubscribeCandles(...)`. As barras finalizadas fornecem os volumes de abertura, máximo, mínimo, fechamento e tick que preenchem o painel.
3. Os melhores valores de lance/venda em tempo real chegam por meio de `SubscribeLevel1(...)`. O spread é recalculado a cada atualização de cotação para espelhar a janela de dados MQL.
4. O bloco formatado é gravado no log de estratégia (`OutputMode = Log`) ou renderizado em um gráfico (`OutputMode = Chart`) com `DrawText(...)`, recriando o painel flutuante de MetaTrader.
5. Chamar `SwapSecurity("TICKER")` durante a execução resolve a nova segurança por meio de `SecurityProvider.LookupById` e inscreve novamente a vela e os feeds de Nível 1 no instrumento solicitado.

A estratégia é apenas informativa; não faz pedidos. Ele pode funcionar de forma independente como um painel de mercado ou junto com outros bots de negociação.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `CandleType` | Período que define a assinatura da vela usada para construir OHLC e dados de volume de ticks. | `TimeFrame(1 minute)` |
| `WatchedSecurityId` | Identificador de instrumento opcional. Deixe em branco para usar `Strategy.Security`. | _vazio_ |
| `OutputMode` | Destino de renderização do bloco de informações. Escolha entre `Chart` (sobreposição perto do preço) ou `Log` (registro de estratégia). | `Chart` |

## Métodos públicos

| Método | Descrição |
|--------|-------------|
| `SwapSecurity(string securityId)` | Resolve o ticker fornecido através do `SecurityProvider` ativo e muda imediatamente o painel para esse símbolo. O método pode ser chamado diversas vezes; cada chamada limpa assinaturas anteriores de vela/nível 1 antes de adicionar os novos feeds. |

## Notas de uso

- Certifique-se de que o conector exponha o identificador solicitado; caso contrário, `SecurityProvider.LookupById` lança uma exceção.
- Quando `OutputMode = Chart`, a estratégia cria automaticamente uma área do gráfico, desenha as velas subscritas e sobrepõe o bloco de status. Para o modo log, apenas as atualizações textuais são produzidas.
- O volume do tick é igual ao `TotalVolume` da vela, que é como MetaTrader relata sua contagem de ticks por barra.
- O spread é mostrado apenas quando o melhor lance e o melhor pedido estão disponíveis. Caso contrário, o campo exibirá `n/a`.

## Detalhes da conversão

- O loop do temporizador MetaTrader é substituído por assinaturas StockSharp. As velas são acionadas uma vez por barra finalizada e as cotações do Nível 1 atualizam o spread em tempo real.
- Os rótulos do painel MQL são representados por um único bloco de texto multilinha. O texto usa a ordem exata da ferramenta original: Tempo, Período, Símbolo, Fechamento, Abertura, Alto, Baixo, Volume do Tick, Spread.
- As trocas de símbolos em tempo de execução não precisam mais de gerenciamento manual do Market Watch – a estratégia resolve os instrumentos diretamente por meio do provedor de segurança StockSharp.
- Somente chamadas API de alto nível são usadas (`SubscribeCandles`, `SubscribeLevel1`, `DrawText`, `AddInfo`). Não há cálculos manuais de indicadores ou manipulações diretas de conectores, satisfazendo as regras de codificação do repositório.
