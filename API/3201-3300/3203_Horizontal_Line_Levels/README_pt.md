# Estratégia de Horizontal Line Levels
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Horizontal Line Levels** emula o consultor especialista de MetaTrader 5 de mesmo nome. Ela reconstrói continuamente dois níveis de preço em torno da cotação atual e notifica o usuário quando o mercado os cruza. A implementação depende de dados de mercado Level1 (bid/ask), imitando o fluxo de trabalho OnTick/OnTimer original sem enviar nenhuma ordem.

## Ideia central

1. Assinar os dados Level1 e armazenar em cache os últimos preços de melhor bid e melhor ask.
2. Converter a distância de pontos do MetaTrader para a escala de preços do StockSharp.
3. Deslocar o melhor ask para cima e o melhor bid para baixo pela distância configurada, criando duas linhas horizontais virtuais.
4. Verificar periodicamente (via temporizador interno) se o bid ou ask cruza esses níveis de referência e registrar alertas no diário da estratégia.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TimerPeriodMinutes` | `1` | Minutos entre duas verificações consecutivas do temporizador. Deve permanecer positivo. |
| `OffsetPoints` | `50` | Distância em pontos MetaTrader aplicada acima do ask e abaixo do bid ao construir as linhas. |

## Detalhes de comportamento

- **Assinatura de dados**: `GetWorkingSecurities` registra um fluxo Level1 para que a estratégia receba atualizações de bid/ask mesmo sem velas.
- **Inicialização**: Na primeira vez que tanto o melhor bid quanto o melhor ask estão disponíveis, `RecalculateLevels` armazena os níveis horizontais atuais superior e inferior.
- **Temporizador**: Cada tick do temporizador recria os níveis ausentes (se a inicialização ocorreu antes de as cotações estarem prontas) e emite mensagens de log quando o mercado viola qualquer um dos limites.
- **Tradução de pontos MetaTrader**: O helper `EnsurePointSize` converte "pontos" MetaTrader em incrementos de preço absolutos usando `Security.PriceStep`. A mesma técnica é usada em outras estratégias convertidas para manter a compatibilidade numérica.
- **Sem trading**: A estratégia nunca envia ordens; produz apenas alertas via `AddInfoLog`. Isso corresponde ao especialista original que exibia alertas pop-up quando o preço tocava qualquer uma das linhas.
- **Parada/Reinicialização**: Parar a estratégia cancela o temporizador e limpa todos os valores em cache para que a próxima execução comece de um estado limpo.

## Uso típico

1. Anexe a estratégia ao instrumento desejado e defina `TimerPeriodMinutes` e `OffsetPoints` na UI do Designer.
2. Inicie a estratégia. Quando um snapshot completo de cotação chegar, uma entrada de log como `Horizontal levels updated. Upper: 1.12345, Lower: 1.12245.` confirma os limiares calculados.
3. Observe a janela de log. Quando o ask sobe acima do nível superior (ou o bid cai abaixo do nível inferior), a estratégia imprime a mensagem de alerta correspondente.
4. Se você alterar o offset ou reiniciar a estratégia, os níveis são recalculados usando os novos parâmetros.

## Classificação

- **Categoria**: Utilitários / Alertas
- **Direção**: Nenhum
- **Estilo de execução**: Monitoramento orientado a eventos
- **Requisitos de dados**: Level1 bid/ask
- **Complexidade**: Básico
- **Período recomendado**: Qualquer (puramente orientado a cotações)
- **Gestão de risco**: Não aplicável (sem posições abertas)

Esta conversão mantém o comportamento centrado em alertas do original MetaTrader enquanto aproveita as abstrações do StockSharp, como temporizadores de estratégia e assinaturas Level1.
