# Estratégia de Controle do Painel de Negociação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Controle do Painel de Negociação** replica a funcionalidade do utilitário "Painel de Negociação" MetaTrader 4 dentro do StockSharp. O painel MQL original permitia que um trader mudasse o período do gráfico ativo e saltasse entre os instrumentos clicando nos botões da interface do usuário. A versão StockSharp expõe os mesmos controles por meio de parâmetros de estratégia para que o aplicativo host (Designer, Terminal ou painel personalizado) possa ajustá-los rapidamente.

Ao contrário do Expert Advisor de origem, esta porta não envia ordens de negociação. Seu objetivo é manter a assinatura do gráfico sincronizada com o período e instrumento atualmente selecionado e registrar os últimos fechamentos de velas, fornecendo feedback semelhante aos rótulos de texto no painel original.

## Conceitos-chave

- **Controle dinâmico de prazo** – escolha entre M1, M5, M15, M30, H1, H4, D1 ou W1. Mudar o parâmetro reconstrói imediatamente a assinatura da vela.
- **Pesquisa de instrumento** – especifique um identificador de segurança a seguir. Quando habilitada, a estratégia pesquisa o `ISecurityProvider` conectado; caso contrário, recairá na segurança já associada à estratégia.
- **Feedback da vela** – cada vela finalizada é registrada com seu preço de fechamento para que o operador possa verificar a combinação ativa de símbolo e período de tempo.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `TimeFrameName` | Código de período preferido (`M1`, `M5`, `M15`, `M30`, `H1`, `H4`, `D1`, `W1`). O padrão é `M15`. |
| `SecurityId` | Identificador opcional do instrumento a controlar. Deixe em branco para utilizar a propriedade `Security` da estratégia. |
| `AutoLookupSecurity` | Quando `true`, a estratégia resolve `SecurityId` até `SecurityProvider`. Desative-o para aceitar a segurança já atribuída como está. |
| `DefaultCandleType` | Fallback `DataType` usado quando um período desconhecido é inserido. O padrão é velas de um minuto. |

## Fluxo de trabalho

1. **Inicialização** – em `OnStarted` a estratégia resolve a segurança e o prazo desejados e, em seguida, inicia uma assinatura de vela para essa combinação.
2. **Ajustes de tempo de execução** – alterar `TimeFrameName`, `SecurityId` ou `AutoLookupSecurity` enquanto a estratégia está em execução reinicia a assinatura com as novas configurações.
3. **Processamento de velas** – cada vela concluída atualiza a propriedade `LastFinishedCandle` e grava uma entrada de registro contendo o identificador de segurança, o código do período e o preço de fechamento.
4. **Desligamento** – as assinaturas são interrompidas durante `OnStopped` ou sempre que a estratégia precisa reconstruí-las porque os parâmetros foram alterados.

## Dicas de uso

- Combine a estratégia com um widget de gráfico no StockSharp Designer para reproduzir o fluxo de trabalho do painel MT4. Editores de parâmetros atuam como botões/combos.
- Deixe `SecurityId` em branco se o host já atribuir um `Security` à instância da estratégia.
- A saída do log pode ser conectada a um rótulo de UI ou console para imitar os rótulos informativos do script original.

## Diferenças da versão MQL

- Sem botões gráficos; alterações de parâmetros são usadas em seu lugar.
- Nenhuma ação de negociação é enviada – a lógica é limitada ao gerenciamento e registro de assinaturas de dados.
- A lista de prazos é idêntica ao painel original, garantindo um comportamento familiar para os traders que migram do MT4.
