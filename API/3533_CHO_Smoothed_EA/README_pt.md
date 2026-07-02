# Estratégia suavizada de CHO EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica a lógica do Expert Advisor "CHO Smoothed EA" original. Ele observa os cruzamentos do oscilador Chaikin em velas concluídas e suaviza o oscilador com uma média móvel configurável. Filtros opcionais limitam a negociação a uma sessão específica, restringem a direção da negociação e validam sinais com confirmação de linha zero. Quando um sinal é aceito, a estratégia envia uma ordem de mercado e gerencia a posição usando distâncias fixas em pontos para stop-loss, take-profit e proteção de trailing.

## Lógica de negociação
- Os valores do oscilador Chaikin são calculados em cada vela finalizada com períodos rápidos e lentos configuráveis.
- Uma média móvel do oscilador cria a linha de sinal. O período e o tipo de média móvel podem ser ajustados.
- Entradas longas ocorrem quando o oscilador cruza acima da linha suavizada. Entradas curtas ocorrem no cruzamento oposto. Os sinais podem ser revertidos para negociar contra a direção original.
- Se o filtro de nível zero estiver habilitado, ambos os valores do oscilador deverão estar abaixo de zero para negociações longas e acima de zero para negociações curtas.
- A estratégia pode fechar automaticamente posições opostas antes de entrar em uma nova negociação ou ignorar sinais até que a posição atual fique estável. Também pode impor um modo de posição única.
- A negociação pode ser restrita a uma janela de tempo diária. Windows que cruzam a meia-noite são suportados.
- Após uma entrada, a estratégia armazena o preço de entrada, converte as distâncias dos pontos configurados em compensações de preço e monitora as velas para eventos de stop-loss, take-profit e trailing-stop.

## Gestão de risco
- Os níveis de stop-loss e take-profit são calculados a partir do preço de entrada usando distâncias de pontos multiplicadas pela etapa de preço do instrumento.
- O trailing stop é ativado após o preço avançar pelo passo final configurado e depois segue na distância final.
- Quando um nível de proteção é atingido, a posição é fechada imediatamente com uma ordem de mercado e todos os níveis de risco são redefinidos.

## Parâmetros
- **Tipo de vela** – período usado para construir as velas para cálculos de indicadores.
- **Período Rápido / Período Lento** – Períodos rápidos e lentos do Oscilador Chaikin.
- **Período MA do sinal / Tipo MA do sinal** – suavização das configurações de média móvel aplicadas ao oscilador.
- **Use Nível Zero** – exige que ambos os valores do oscilador estejam no lado correto de zero antes de negociar.
- **Modo de negociação** – permite apenas posições compradas, apenas vendidas ou ambas as direções.
- **Sinais Reversos** – troque entradas longas e curtas.
- **Fechar Oposto** – feche posições opostas existentes antes de abrir uma nova negociação.
- **Apenas uma posição** – evita entradas quando uma posição já está aberta.
- **Use Controle de Tempo / Hora de Início / Hora de Término** – habilite e configure a janela de negociação diária.
- **Stop Loss (pts)** – distância em pontos para o stop de proteção.
- **Take Profit (pts)** – distância em pontos para metas de lucro.
- **Trailing Stop (pts)** – distância do trailing stop em pontos.
- **Trailing Step (pts)** – movimento mínimo favorável (em pontos) antes de mover o trailing stop.

## Notas adicionais
- Defina a propriedade `Volume` da estratégia antes de iniciá-la para controlar o tamanho da negociação.
- Como a estratégia emite ordens de mercado, garanta liquidez suficiente e considere derrapagens em ambientes reais.
- Quando os horários de início e término da janela de negociação são iguais, a estratégia permanece inativa, correspondendo ao comportamento original do Expert Advisor.
