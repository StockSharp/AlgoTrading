# Estratégia de Som de Barra Grande
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Som de Barra Grande** reproduz o comportamento do consultor especialista do MetaTrader "BigBarSound". O algoritmo observa velas terminadas de um período de tempo configurável e reporta quando o intervalo da vela é amplo o suficiente para ser considerado uma "barra grande". Em vez de reproduzir um arquivo de áudio, escreve mensagens de log detalhadas que podem ser roteadas para qualquer subsistema de notificação suportado pelo StockSharp.

A estratégia é puramente informativa – não envia ordens nem gerencia posições. É projetada para ser usada como componente de alerta dentro de um fluxo de trabalho de trading automatizado ou discricionário maior.

## Comportamento
1. A estratégia se inscreve na série de velas especificada pelo parâmetro **Tipo de vela**.
2. Para cada vela concluída, mede o tamanho da barra de acordo com o **Modo de diferença** selecionado:
   - **OpenClose** – diferença absoluta entre o preço de fechamento e o de abertura.
   - **HighLow** – diferença absoluta entre a máxima e a mínima da barra.
3. O valor medido é comparado com o **Limiar de pontos** multiplicado pelo `PriceStep` do instrumento. Quando o tamanho da barra é maior ou igual a esse limiar, a estratégia registra uma entrada de log que simula reproduzir o arquivo de som configurado.
4. Se **Mostrar alerta** estiver ativado, uma mensagem de log adicional no estilo alerta é escrita para destacar o evento.

Como a implementação processa apenas velas terminadas, cada barra pode disparar no máximo uma vez, espelhando o comportamento de disparo único do consultor especialista MQL original.

## Parâmetros
- **Limiar de pontos (`BarPoint`)** – número de passos de preço que devem ser excedidos antes que um alerta seja disparado. O valor padrão de 200 corresponde ao script original. Limites de otimização (50–500 com passo 50) são fornecidos para conveniência.
- **Modo de diferença (`DifferenceMode`)** – seleciona como o tamanho da vela é medido: distância abertura/fechamento ou intervalo completo máxima/mínima.
- **Arquivo de som (`SoundFile`)** – nome do arquivo WAV que deve ser reproduzido. A estratégia apenas registra esse valor para emular a chamada `PlaySound` do MetaTrader.
- **Mostrar alerta (`ShowAlert`)** – quando ativado, a estratégia emite uma mensagem de log adicional para imitar o popup opcional `Alert` da versão MQL.
- **Tipo de vela (`CandleType`)** – tipo de dados de vela (período de tempo) para se inscrever. Por padrão a estratégia usa velas de 1 minuto.

## Alertas e registro
A estratégia usa `LogInfo` para anunciar que o arquivo de som teria sido reproduzido e `AddInfoLog` para fornecer uma mensagem de alerta separada. Essas entradas contêm o identificador do instrumento, o timestamp da vela e o tamanho medido da barra, facilitando a integração com os visualizadores de log ou destinos de notificação do StockSharp.

Se o broker não fornecer um `PriceStep` válido, um valor de fallback de `1` é usado para que a estratégia permaneça operacional. Ajuste o **Limiar de pontos** de acordo para refletir o tamanho real do tick do instrumento.

## Notas de uso
- Anexe a estratégia a qualquer instrumento que exponha dados de velas. O alerta funciona igualmente bem em forex, futuros, ações ou ativos cripto.
- Combine-a com outras estratégias de trading inscrevendo-se na saída de log ou estendendo a classe para encaminhar eventos a manipuladores personalizados.
- Como a implementação não gera ordens, `Volume` e parâmetros relacionados a posição são ignorados.
- Para produzir notificações audíveis, conecte o subsistema de registro do StockSharp a um notificador de som ou estenda o código para chamar APIs de áudio específicas da plataforma.

## Diferenças em relação ao consultor especialista MQL original
- O script original operava com dados de ticks e rastreava as mudanças de barra manualmente. O port do StockSharp processa velas terminadas diretamente, o que garante exatamente um alerta por barra sem manter uma flag de disparo separada.
- A reprodução de áudio é substituída por mensagens de log para que o comportamento permaneça multiplataforma dentro do ambiente StockSharp.
- Os nomes de parâmetros seguem as convenções do StockSharp, mas retêm a mesma semântica: tamanho limiar em pontos, modo de medição, alerta opcional e nome de som.

## Requisitos
Nenhum indicador adicional é necessário. Simplesmente certifique-se de que o `CandleType` selecionado seja suportado pela fonte de dados conectada para que a estratégia receba velas concluídas para processar.
