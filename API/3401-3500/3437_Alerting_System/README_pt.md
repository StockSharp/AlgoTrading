# Estratégia do Sistema de Alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia do **Sistema de Alerta** é uma conversão StockSharp fiel do MetaTrader 4 consultor especialista `AlertingSystem.mq4`. O script original desenha duas linhas horizontais e emite um som sempre que o mercado as toca. A versão StockSharp atinge o mesmo objetivo assinando cotações de Nível 1 (melhor oferta/venda) e imprimindo mensagens de diário quando qualquer nível de alerta configurável é ultrapassado.

## Ideia Central

1. Registre um fluxo de dados de nível 1 para que a estratégia receba atualizações de lance e solicitação tick-by-tick, espelhando o manipulador MQL `OnTick`.
2. Leia os níveis `UpperPrice` e `LowerPrice` definidos pelo usuário. Um valor de `0` desativa o alerta correspondente, assim como remover a linha horizontal em MetaTrader.
3. Compare cada oferta recebida com o nível superior e cada solicitação com o nível inferior.
4. Emita uma única notificação de registro quando o preço ultrapassar um nível ativo e espere até que o mercado retorne à zona segura antes de armar o alerta novamente. Isso evita alertas duplicados ruidosos, preservando ao mesmo tempo a intenção do acionador sonoro original.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `UpperPrice` | `0` | Nível de alerta horizontal superior. Defina como `0` para desativar a verificação. |
| `LowerPrice` | `0` | Nível de alerta horizontal inferior. Defina como `0` para desativar a verificação. |

Ambos os parâmetros são expostos por meio da UI do Designer. Eles podem ser alterados antes do lançamento ou durante a execução da estratégia; a próxima atualização de cotação usará os novos níveis.

## Comportamento de tempo de execução

- **Assinatura de dados**: `GetWorkingSecurities` solicita dados de nível 1, garantindo que a estratégia receba atualizações de compra/venda mesmo sem velas ou negociações.
- **Inicialização**: Quando `OnStarted` é acionado, a estratégia registra os níveis atualmente configurados para que o operador possa verificar a configuração.
- **Detecção de alertas**: métodos auxiliares (`CheckUpperAlert` e `CheckLowerAlert`) armazenam sinalizadores internos para garantir que cada violação produza exatamente uma notificação até que o mercado volte além do limite.
- **Sem negociação**: A conversão não envia ordens. É puramente um utilitário de alerta, correspondendo ao comportamento do script MetaTrader que reproduzia apenas um som.
- **Tratamento de redefinição**: `OnReseted` limpa os sinalizadores internos para que a próxima execução comece com novos estados de alerta.

## Etapas típicas de uso

1. Selecione o instrumento desejado no StockSharp Designer e anexe `AlertingSystemStrategy`.
2. Especifique os níveis de alerta superior e/ou inferior. Deixe um valor em `0` para ignorar esse lado.
3. Comece a estratégia. O log exibirá entradas confirmando quais alertas estão ativos.
4. Monitore a janela do diário. Quando o lance ultrapassa o nível superior ou o pedido cai abaixo do nível inferior, a estratégia registra uma mensagem descritiva.

## Notas de conversão

- O consultor MetaTrader original criou duas linhas horizontais arrastáveis. StockSharp usa parâmetros numéricos, o que mantém o fluxo de trabalho determinístico e mais adequado para execução algorítmica.
- MetaTrader acionou a função `PlaySound` em cada tick qualificado. Para evitar sobrecarregar o log, a conversão desativa os alertas até que o preço entre novamente na faixa aceitável.
- A lógica permanece intencionalmente livre de indicadores: apenas cotações brutas são necessárias, portanto a estratégia funciona em qualquer período ou instrumento que forneça dados de Nível 1.

## Classificação

- **Categoria**: Utilitários / Alertas
- **Direção de Negociação**: Nenhuma
- **Estilo de execução**: monitoramento orientado a eventos
- **Requisitos de dados**: oferta/venda de nível 1
- **Complexidade**: Básico
- **Prazo recomendado**: Qualquer (baseado em cotação)
- **Gerenciamento de Riscos**: Não aplicável (nenhuma posição aberta)

Esta documentação resume a implementação do StockSharp e destaca as etapas práticas necessárias para reproduzir o fluxo de trabalho de alertas do MetaTrader dentro da plataforma.
