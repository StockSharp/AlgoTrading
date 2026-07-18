# Estratégia Auto Stop-Loss e Take-Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utilitária anexa automaticamente ordens protetoras de stop-loss e take-profit a cada posição aberta no instrumento configurado. Ela espelha o comportamento do expert original do MetaTrader "AutoSet SL TP", monitorando a lista de posições ativas e aplicando as restrições de distância do corretor antes de registrar as ordens protetoras.

A estratégia não abre trades por conta própria. Em vez disso, ela monitora o volume, direção e preço de execução das posições que foram criadas manualmente ou por outras estratégias. Assim que uma posição longa ou curta aparecer, o algoritmo calcula os níveis desejados de stop-loss e take-profit expressos em pips no estilo MetaTrader, ajusta os níveis para cumprir com as restrições de congelamento e parada publicadas pela bolsa, e então envia as ordens protetoras de mercado apropriadas. Quando a posição é totalmente fechada, as ordens protetoras são canceladas automaticamente.

## Como funciona

1. Assina dados de Nível1 para receber os melhores preços bid/ask juntamente com os campos opcionais `StopLevel` e `FreezeLevel` fornecidos pelo corretor.
2. Converte as distâncias configuradas em pips para preços absolutos usando os metadados do símbolo (passo de preço e precisão decimal). Cotações de cinco e três dígitos são automaticamente escalonadas por um fator de dez para corresponder à semântica de pip do MetaTrader.
3. Em cada atualização de cotação ou notificação de trade pessoal:
   - Ignora o sinal se não houver posição aberta ou se a direção não corresponder ao filtro configurado (somente compra, somente venda ou ambos).
   - Calcula a distância mínima permitida entre o preço de mercado e uma ordem protetora. Se o corretor não publicar níveis de congelamento/parada, o algoritmo recorre a três spreads multiplicados por 1.1 para se manter com segurança fora das zonas proibidas.
   - Determina o preço de stop-loss e take-profit em relação ao ask atual (para longos) ou bid (para curtos) e normaliza o resultado para o passo de preço do instrumento.
   - Coloca ou re-registra ordens protetoras de stop ou limite com o volume exato da posição. As ordens são substituídas apenas quando o preço alvo ou o volume muda, o que mantém as modificações na bolsa no mínimo.
4. Se o volume da posição se tornar zero, todas as ordens protetoras pendentes são canceladas. A estratégia também cancela as ordens existentes quando a direção do trade não é mais permitida pelo filtro.

Como o algoritmo depende exclusivamente de fills externos, ele pode ser combinado com negociação discricionária, painéis ou outros sistemas automatizados que gerenciam entradas, enquanto esta estratégia garante um envelope protetor consistente.

## Parâmetros

- **`StopLossPips`** – distância do preço de mercado atual ao stop-loss em pips do MetaTrader. Um valor de `0` desabilita a ordem de stop. Padrão: `50`.
- **`TakeProfitPips`** – distância do preço de mercado atual ao take-profit em pips do MetaTrader. Um valor de `0` desabilita a ordem de take-profit. Padrão: `140`.
- **`DirectionFilter`** – especifica qual direção de posição é gerenciada:
  - `Buy` – proteger apenas exposição longa.
  - `Sell` – proteger apenas exposição curta.
  - `BuySell` – proteger ambos os lados (comportamento padrão no script original).

## Notas práticas

- As ordens protetoras são sempre criadas com o volume absoluto da posição. Se o corretor impuser tamanhos de lote mínimos ou máximos, a estratégia arredonda o volume para o valor permissível mais próximo antes de colocar as ordens.
- O algoritmo usa `ReRegisterOrder` para ajustar ordens protetoras ativas. Isso mantém os mesmos identificadores de ordem da bolsa sempre que possível e evita cancelamentos desnecessários.
- A distância de fallback (spread × 3 × 1.1) evita que o stop ou take-profit viole restrições ocultas da bolsa quando os níveis explícitos de congelamento/parada não são fornecidos.
- Como a estratégia não gerencia entradas, ela pode ser iniciada antes ou depois de as posições serem abertas. Qualquer posição qualificada que já exista no momento da inicialização será protegida imediatamente após a primeira atualização de cotação.
- Os "pips" do MetaTrader diferem dos passos de preço da bolsa em símbolos com três ou cinco dígitos decimais. A estratégia replica o Expert Advisor original multiplicando o valor do ponto de acordo, garantindo que os números configurados correspondam exatamente às configurações do MT5.

## Diferenças em relação ao expert do MetaTrader

- Em vez de modificar os atributos de stop e take-profit na posição, o StockSharp gerencia ordens protetoras de stop e limite explícitas. Essa abordagem mantém a lógica completamente transparente dentro do livro de ordens do StockSharp.
- A versão do StockSharp usa dados de mercado de Nível1 para reconstruir os níveis de restrição do corretor. Se o provedor expuser nomes de campo diferentes para distâncias de congelamento ou parada, a estratégia os descobre automaticamente por reflexão no enum `Level1Fields`.
- Cada comentário de código e mensagem de log está em inglês para manter a consistência com as diretrizes de codificação, enquanto a documentação é localizada em russo e chinês para os usuários finais.
