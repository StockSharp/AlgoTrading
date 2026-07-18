# Estratégia de limite do sistema de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Limite do Sistema de Alerta** é uma porta StockSharp do MetaTrader 5 consultor especialista "AlertingSystem" (MQL pasta `31843`). O EA original desenha duas linhas horizontais e emite um som sempre que o lance é negociado acima da linha superior ou o pedido é negociado abaixo da linha inferior. Essa conversão C# mantém o comportamento de alerta ao usar o StockSharp de alto nível do API para acesso a dados e registro de notificação.

## Ideia Central

* Ouça dados de mercado de Nível 1 em tempo real (melhor oferta e melhor venda).
* Acione alertas únicos quando o lance for maior ou igual a um limite superior configurável.
* Acione alertas únicos quando a solicitação for menor ou igual a um limite inferior configurável.
* Redefina os sinalizadores de alerta quando os preços voltarem para dentro da banda para que o próximo rompimento possa ser detectado.

Ao contrário da implementação MQL que reproduz repetidamente um som a cada tick, a versão StockSharp envia uma única entrada de registro informativa para cada evento de breakout. Isso evita a inundação de registros e ainda notifica a operadora quando as metas de preço são atingidas.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `UpperPrice` | Nível de lance que ativa o alerta de alta. Defina como `0` para desativar. | `0` |
| `LowerPrice` | Nível de venda que ativa o alerta de baixa. Defina como `0` para desativar. | `0` |

Ambos os parâmetros são valores `StrategyParam<decimal>` padrão que podem ser otimizados ou ajustados em tempo de execução. Você pode mover os limites durante a negociação ao vivo, da mesma forma que reposicionaria as linhas horizontais em MetaTrader.

## Assinaturas de dados e fluxo de trabalho

1. Quando a estratégia é iniciada, ela assina os dados do Nível 1 via `SubscribeLevel1().Bind(ProcessLevel1).Start()`.
2. Os objetos `Level1ChangeMessage` recebidos atualizam os melhores valores de lance e melhor pedido armazenados em cache.
3. Cada atualização chama as verificações de alerta:
   * **Alerta superior** – dispara uma vez quando `BestBid >= UpperPrice` e o preço estava anteriormente abaixo do nível.
   * **Alerta inferior** – dispara uma vez quando `BestAsk <= LowerPrice` e o preço estava anteriormente acima do nível.
4. As redefinições de alerta ocorrem automaticamente quando o mercado volta a negociar dentro do corredor.

## Registro e notificações

Os alertas são escritos com `AddInfoLog` e incluem os valores atuais de compra/venda e os níveis configurados. Integre seu próprio pipeline de notificação (e-mails, mensageiros, sons personalizados) substituindo `OnInfo` ou inscrevendo-se nos eventos de registro da estratégia em seu aplicativo de hospedagem.

## Dicas de uso

* Defina apenas os limites de seu interesse – o outro pode permanecer `0` para permanecer desativado.
* Combine a estratégia com outros módulos que reagem aos registros `Info` se desejar reproduzir notificações sonoras ou push.
* Como a estratégia nunca faz pedidos, não há necessidade de ligar para `StartProtection()`.

## Diferenças do original EA

* A versão StockSharp usa dados de nível 1 em vez de criar objetos gráficos.
* Os alertas são únicos por breakout para manter o log limpo.
* Todo o resto (parâmetros, limites lógicos, condições) corresponde à referência MQL.

## Arquivos

* `CS/AlertingSystemStrategy.cs` – Implementação de estratégia C#.
* `README.md` – Documentação em inglês (este arquivo).
* `README_ru.md` – Tradução russa com explicação adicional.
* `README_zh.md` – Tradução simplificada para chinês.
